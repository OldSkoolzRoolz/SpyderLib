using System.Diagnostics;

using CommunityToolkit.Diagnostics;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public interface IDownloadController
{
    #region Public Methods

    void Start();

    #endregion
}



public class DownloadController : IDownloadController
{
    [NotNull] private readonly ICacheIndexService _cache;
    private HashSet<string> _downloadedLinks;
    [NotNull] private readonly IBackgroundDownloadQue _downloadQue;
    [NotNull] private readonly ILogger _logger;
    private TaskCompletionSource _longRunningTaskLifetime;
    [NotNull] private readonly SpyderOptions _options;
    private readonly object _readLock = new();
    private HashSet<string> _searchedPages;
    private readonly object _writeLock = new();
    private const string DLOADED_PATH = "/Storage/Spyder/Logs";






    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="downloadQue"></param>
    /// <param name="cacheIndexService"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DownloadController(
        IBackgroundDownloadQue downloadQue,
        ICacheIndexService cacheIndexService)
        {
            var fact = (ILoggerFactory)AppContext.GetData(name: "factory");
            Guard.IsNotNull(value: fact);
            _logger = fact.CreateLogger<DownloadController>();

            Debug.WriteLine(message: "Download Controller Initialized!");

            _options = AppContext.GetData(name: "options") as SpyderOptions ??
                       throw new ArgumentNullException(nameof(_options));

            _downloadQue = downloadQue ?? throw new ArgumentNullException(nameof(downloadQue));
            _cache = cacheIndexService ?? throw new ArgumentNullException(nameof(cacheIndexService));


            _ = StartupComplete.TrySetResult(true);
        }






    #region Properteez

    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    #region Public Methods

    public void Deconstruct(
        [NotNull] out ICacheIndexService cache,
        [NotNull] out IBackgroundDownloadQue downloadQue,
        [NotNull] out ILogger logger,
        [NotNull] out SpyderOptions options,
        out HashSet<string> searchedPages,
        out TaskCompletionSource longRunningTaskLifetime)
        {
            cache = _cache;
            downloadQue = _downloadQue;
            logger = _logger;
            options = _options;
            searchedPages = _searchedPages;
            longRunningTaskLifetime = _longRunningTaskLifetime;
        }






    /// <summary>
    ///     Searches soutce code of 200 pages in local cache for the htmlElement specified in SpyderOptions
    ///     and creates a downloadItem to the background downloader.
    /// </summary>
    public async Task ProducerLoaderAsync()
        {
            var avail = _cache.CacheIndexItems.Where(p => !_searchedPages.Contains(item: p.Value)).ToList();

            foreach (var page in avail)
                {
                    _searchedPages.Add(item: page.Value);
                    // Add page to file so we skip nexttime
                    AppendLineToFile(fileName: "SearchedPages.txt", line: page.Value);

                    //Get source
                    var pageSource = await GetCachedPageSourceFromDisk(page: page).ConfigureAwait(false);

                    //Checks source for HtmlTag and returns any found source links
                    var found = HtmlParser.TryExtractUserTagFromDocument(
                        HtmlParser.CreateHtmlDocument(content: pageSource),
                        tagToSearchFor: _options.HtmlTagToSearchFor,
                        out var foundLinks);

                    //  If there are any links found they are checked against the completed list
                    // and added to the download Que if they are new.
                    if (found)
                        {
                            await FilterAndAddToQueue(foundLinks: foundLinks, page: page).ConfigureAwait(false);
                        }
                }

            // Closses the buffer block and should trigger the downloading of the que
            await _downloadQue.Complete();
            _logger.SpyderDebug($"Finished Adding download tasks. Que Count {_downloadQue.Count}");
        }






    public void SetInputComplete() { }






    public void Start()
        {
            Init();
        }

    #endregion






    #region Private Methods

    internal async Task AddDownloadItem(DownloadItem item)
        {
            await _downloadQue.QueueBackgroundWorkItemAsync(workItem: item).ConfigureAwait(false);



            // Link not found in hash so we will add it to the file.
            AppendLineToFile(fileName: "DownloadedLinks.txt", line: item.Link);

            _logger.SpyderDebug($"download que tasks available == {_downloadQue.Count}");
        }






    private void AppendLineToFile(string fileName, string line)
        {
            var filePath = Path.Combine(path1: DLOADED_PATH, path2: fileName);
            try
                {
                    lock (_writeLock)
                        {
                            using (var writer = File.AppendText(path: filePath))
                                {
                                    writer.WriteLine(value: line);
                                    writer.Flush();
                                }
                        }
                }
            catch (IOException ex)
                {
                    Console.WriteLine(Resources1.IO_Write_Error + ex.Message);
                }
            catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine(Resources1.Permission_Error_Writing_File + ex.Message);
                }
            catch (ArgumentException ex)
                {
                    Console.WriteLine(Resources1.File_Path_Invalid + ex.Message);
                }
        }






    /// <summary>
    ///     This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The
    ///     implementation should return a task that represents
    ///     the lifetime of the long running operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">
    ///     Triggered when
    ///     <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is
    ///     called.
    /// </param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
    /// <remarks>
    ///     See <see href="https://docs.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for
    ///     implementation guidelines.
    /// </remarks>
    protected Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _longRunningTaskLifetime = new(state: stoppingToken);

            return _longRunningTaskLifetime.Task;
        }






    private async Task FilterAndAddToQueue(ConcurrentScrapedUrlCollection foundLinks, KeyValuePair<string, string> page)
        {
            foreach (var s in foundLinks.Select(k => k.Key).Except(second: _downloadedLinks))
                {
                    try
                        {
                            await AddDownloadItem(new(link: s, savePath: _options.OutputFilePath))
                                .ConfigureAwait(false);
                        }
                    catch (SpyderException)
                        {
                            //Remove the last entry that caused exception
                            _ = _cache.CacheIndexItems.Remove(key: page.Key, value: out _);
                        }
                }
        }






    private async Task<string> GetCachedPageSourceFromDisk(KeyValuePair<string, string> page)
        {
            var pagePath = Path.Combine(path1: _options.CacheLocation, path2: page.Value);
            var source = await File.ReadAllTextAsync(path: pagePath).ConfigureAwait(false);


            return source;
        }






    private void Init()
        {
            _logger.SpyderInfoMessage(message: "Download controller initializizing");
            LoadDownloadedLinksListFromDisk();
            LoadSearchedPagesFromDisk();
            _logger.SpyderInfoMessage(message: "Download controller initialized, searching for downloadables in cache");
            Task.Run(function: ProducerLoaderAsync).ConfigureAwait(false);
        }






    private void LoadDownloadedLinksListFromDisk()
        {
            var path = Path.Combine(path1: DLOADED_PATH, path2: "DownloadedLinks.txt");
            if (!File.Exists(path: path))
                {
                    _ = File.CreateText(path: path);
                    _downloadedLinks = new();
                }


            lock (_readLock)
                {
                    var set = new HashSet<string>(File.ReadAllLines(path: path));

                    _downloadedLinks = set;
                }
        }






    private void LoadSearchedPagesFromDisk()
        {
            var path = Path.Combine(path1: DLOADED_PATH, path2: "SearchedPages.txt");

            if (!File.Exists(path: path))
                {
                    _ = File.CreateText(path: path);

                    _searchedPages = new();
                }


            lock (_readLock)
                {
                    var set = new HashSet<string>(File.ReadAllLines(path: path));

                    _searchedPages = set;
                }
        }

    #endregion
}