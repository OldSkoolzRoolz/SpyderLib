using System.Collections.Concurrent;
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

public static event EventHandler<EventArgs> DownloadQueueLoadComplete;




    /// <summary>
    /// 
    /// </summary>
    /// <param name="downloadQue"></param>
    /// <param name="cacheIndexService"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DownloadController(
        IBackgroundDownloadQue downloadQue,
        ICacheIndexService cacheIndexService)
        {
            var fact = (ILoggerFactory)AppContext.GetData("factory");
            Guard.IsNotNull(fact);
            _logger = fact.CreateLogger<DownloadController>();

            Console.WriteLine("Download Controller Initialized!");

            _options = AppContext.GetData("options") as SpyderOptions ??
                       throw new ArgumentException("_options");

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
    ///     and creates a downloadItem task to the downloadQue 
    ///     
    /// </summary>
    public async Task ProducerLoaderAsync()
        {
            var avail = ExcludeDictionaryByValues(_cache.CacheIndexItems, _searchedPages);

         
         
            foreach (var page in avail)
                {
                    _searchedPages.Add(page.Value);
                    // Add page to file so we skip nexttime
                    AppendLineToFile("SearchedPages.txt", page.Value);

                    //Get source
                    var pageSource = await GetCachedPageSourceFromDisk(page).ConfigureAwait(false);

                    //Checks source for HtmlTag and returns any found source links
                    var found = HtmlParser.TryExtractUserTagFromDocument(
                        HtmlParser.CreateHtmlDocument(pageSource),
                        _options.HtmlTagToSearchFor,
                        out var foundLinks);

                    //  If there are any links found they are checked against the completed list
                    // and added to the download Que if they are new.
                    if (found)
                        {
                            await FilterAndAddToQueue(foundLinks, page).ConfigureAwait(false);
                        }
                }

            // Closses the buffer block and should trigger the downloading of the que
            await _downloadQue.Complete().ConfigureAwait(true);

            DownloadQueueLoadComplete?.Invoke(null, _downloadQue.Count);

        }





    private static Dictionary<TKey, TValue> ExcludeDictionaryByValues<TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> dictionary,
        HashSet<TValue> valuesToExclude)
        {
            return dictionary.Where(entry => !valuesToExclude.Contains(entry.Value))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
        }



    public void Start()
        {
            Init();
        }

    #endregion






    #region Private Methods

    internal async Task AddDownloadItem(DownloadItem item)
        {
            await _downloadQue.QueueBackgroundWorkItemAsync(item).ConfigureAwait(false);



            // Link not found in hash so we will add it to the file.
            AppendLineToFile("DownloadedLinks.txt", item.Link);

            _logger.SpyderDebug($"download que tasks available == {_downloadQue.Count}");
        }






    private void AppendLineToFile(string fileName, string line)
        {
            var filePath = Path.Combine(DLOADED_PATH, fileName);
            try
                {
                    lock (_writeLock)
                        {
                            using (var writer = File.AppendText(filePath))
                                {
                                    writer.WriteLine(line);
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
            _longRunningTaskLifetime = new(stoppingToken);

            return _longRunningTaskLifetime.Task;
        }






    private async Task FilterAndAddToQueue(ConcurrentScrapedUrlCollection foundLinks, KeyValuePair<string, string> page)
        {
            foreach (var s in foundLinks.Select(k => k.Key).Except(_downloadedLinks))
                {
                    try
                        {
                            await AddDownloadItem(new(s, _options.OutputFilePath))
                                .ConfigureAwait(false);
                        }
                    catch (SpyderException)
                        {
                            //Remove the last entry that caused exception
                            _ = _cache.CacheIndexItems.Remove(page.Key, out _);
                        }
                }
        }






    private async Task<string> GetCachedPageSourceFromDisk(KeyValuePair<string, string> page)
        {
            var pagePath = Path.Combine(_options.CacheLocation, page.Value);
            var source = await File.ReadAllTextAsync(pagePath).ConfigureAwait(false);


            return source;
        }






    /// <summary>
    /// Initializes the download controller. This method must be called before the controller can be used.
    /// </summary>
    private void Init()
    {



        // Log the fact that the controller is initializing
        _logger.SpyderInfoMessage("Download controller initializizing");

        // Load the set of links that have already been downloaded from disk
        LoadDownloadedLinksListFromDisk();

        // Load the list of pages that have already been searched from disk
        LoadSearchedPagesFromDisk();

        // Log that the controller has finished initializing and is ready for use
        _logger.SpyderInfoMessage("Download controller initialized, searching for downloadables in cache");

        // Start a new task to search for downloadable items in the cache
        Task.Run(ProducerLoaderAsync).ConfigureAwait(false);
    }

  






    /// <summary>
    ///     Loads the list of links that have already been downloaded from disk.
    /// </summary>
    private void LoadDownloadedLinksListFromDisk()
    {
        var path = Path.Combine(DLOADED_PATH, "DownloadedLinks.txt");
        // If file doesn't exist create it and initialize the set with an empty set.
        if (!File.Exists(path))
        {
            var x = File.CreateText(path);
            x.Dispose();
            _downloadedLinks = new HashSet<string>();
        }

        lock (_readLock)
        {
            // Read all lines from the file and create a new set with them.
            var set = new HashSet<string>(File.ReadAllLines(path));

            // Assign the set to the downloaded links field.
            _downloadedLinks = set;
        }
    }







    /// <summary>
    ///     Loads the set of pages that have already been searched from disk.
    /// </summary>
    private void LoadSearchedPagesFromDisk()
    {
        var path = Path.Combine(DLOADED_PATH, "SearchedPages.txt");

        // If file doesn't exist create it and initialize the set with an empty set.
        if (!File.Exists(path))
        {
            var y = File.CreateText(path);
            y.Dispose();

            _searchedPages = new HashSet<string>();
        }


        lock (_readLock)
        {
            // Read all lines from the file and create a new set with them.
            var set = new HashSet<string>(File.ReadAllLines(path));

            // Assign the set to the searched pages field.
            _searchedPages = set;
        }
    }

    #endregion
}