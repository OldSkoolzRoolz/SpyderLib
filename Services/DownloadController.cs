using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

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
    #region feeeldzzz

    private const string DOWNLOADED_PATH = "/Storage/Spyder/Logs";
    [NotNull] private readonly ICacheIndexService _cache;
    [NotNull] private readonly IBackgroundDownloadQue _downloadQue;
    [NotNull] private readonly ILogger _logger;
    [NotNull] private readonly SpyderOptions _options;
    private readonly object _readLock = new();
    private readonly object _writeLock = new();
    private HashSet<string> _downloadedLinks;
    private TaskCompletionSource _longRunningTaskLifetime;
    private HashSet<string> _searchedPages;

    #endregion






    /// <summary>
    ///     Fires when the download queue has finished loading and tasks can be started
    /// </summary>
    public static event EventHandler<EventArgs> DownloadQueueLoadComplete;

    /// <summary>
    ///     Fires when the download controller has finished
    /// </summary>
    public static event EventHandler<EventArgs> DownloadsCompleted;

    /// <summary>
    ///     Fires when the download controller has finished initializing
    /// </summary>
    public static event EventHandler<EventArgs> InitializationComplete;






    /// <summary>
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

            Console.WriteLine(Resources1.DownloadController_DownloadController_Download_Controller_Initialized_);

            _options = AppContext.GetData("options") as SpyderOptions ??
                       throw new ArgumentException("_options");

            _downloadQue = downloadQue ?? throw new ArgumentNullException(nameof(downloadQue));
            _cache = cacheIndexService ?? throw new ArgumentNullException(nameof(cacheIndexService));
            InitializationComplete += OnInitComplete;
            DownloadQueueLoadComplete += OnQueueLoadComplete;
            DownloadsCompleted += OnDownloadsComplete;
        }






    #region Properteez

    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    private void OnDownloadsComplete(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }






    private void OnQueueLoadComplete(object sender, EventArgs e)
        {
            Log.Debug("Download Queue Load Complete Tasks Available == {0}",
                _downloadQue.Count.ToString(CultureInfo.InvariantCulture));
            _downloadQue.PostingComplete();
        }






    private void OnInitComplete(object sender, EventArgs e)
        {
            StartupComplete.TrySetResult(true);
        }






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
    ///     <b>avail - pages that have not been searched</b>
    ///     Searches source code of 200 pages in local cache for the htmlElement specified in SpyderOptions
    ///     and creates a downloadItem task to the downloadQue
    /// </summary>
    public async Task LoadProducerQueueAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Guard.IsNotNull(_cache.CacheIndexItems);

            // Gets the list of pages that have not already been searched from disk
            var unsearchedCache = ExcludeDictionaryByValues(_cache.CacheIndexItems, _searchedPages);

            if (unsearchedCache.Count == 0)
                {
                    DownloadQueueLoadComplete?.Invoke(null, EventArgs.Empty);
                    return;
                }

            // Creates a task for each page
            IEnumerable<Task> processPageTasks = from url in unsearchedCache select ProcessCachedPageAsync(url);

            var taskList = processPageTasks.ToList();

            while (taskList.Count > 0)
                {
                    var finishedTask = await Task.WhenAny(taskList).ConfigureAwait(false);
                    await finishedTask.ConfigureAwait(false);
                    taskList.Remove(finishedTask);
                }

            stopwatch.Stop();

            // ReSharper disable once LocalizableElement
            // TODO:  Temporary message  remove
            Console.WriteLine($"Elapsed time:          {stopwatch.Elapsed}\n");

            // fires the event that the download queue has finished loading
            DownloadQueueLoadComplete?.Invoke(null, EventArgs.Empty);
        }






    private async Task<CachedPage> ProcessCachedPageAsync(CachedPage page)
        {
            // Add page to file so we skip nexttime
            //  _searchedPages.Add(page.Value);
            AppendLineToFile("SearchedPages.txt", page.CachedPageInfo.Value);

            //Get source
            var pageSource = GetCachedPageSourceFromDiskAsync(page).ConfigureAwait(false);

            //Checks source for HtmlTag and returns any found source links
            var found = HtmlParser.TryExtractUserTagFromDocument(
                HtmlParser.CreateHtmlDocument(await pageSource),
                _options.HtmlTagToSearchFor,
                out var foundLinks);


            //  If there are any links found they are checked against the completed list
            // and added to the download Que if they are new.
            if (found)
                {
                    await FilterAndAddToQueueAsync(foundLinks, page).ConfigureAwait(false);
                }

            return await Task.FromResult(page).ConfigureAwait(false);
        }






    /// <summary>
    ///     Excludes all entries from a ConcurrentDictionary where the value is contained in the valuesToExclude HashSet.
    ///     This is done in a thread-safe manner.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary key</typeparam>
    /// <typeparam name="TValue">Type of the dictionary value</typeparam>
    /// <param name="dictionary">The dictionary to remove the entries from</param>
    /// <param name="valuesToExclude">The values to exclude</param>
    /// <returns>A new dictionary without the excluded entries</returns>
    private static List<CachedPage> ExcludeDictionaryByValues<TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> dictionary,
        HashSet<TValue> valuesToExclude)
        {
            return dictionary.Where(entry => !valuesToExclude.Contains(entry.Value))
                .Select(entry => new CachedPage(entry.Key.ToString(), entry.Value.ToString()))
                .ToList();
        }






    public void Start()
        {
            Init();
        }

    #endregion






    #region Private Methods

    internal async Task AddDownloadItemAsync(DownloadItem item)
        {
            await _downloadQue.QueueBackgroundWorkItemAsync(item).ConfigureAwait(false);



            // Link not found in hash so we will add it to the file.
            AppendLineToFile("DownloadedLinks.txt", item.Link);

            _logger.SpyderDebug($"download que tasks available == {_downloadQue.Count}");
        }






    private static void AppendLineToFile(string fileName, string line)
        {
            var filePath = Path.Combine(DOWNLOADED_PATH, fileName);
            try
                {
                    using var writer = File.AppendText(filePath);
                    writer.WriteLine(line);
                    writer.Flush();
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
    ///     This method is called when the <see cref="Microsoft.Extensions.Hosting.IHostedService" /> starts. The
    ///     implementation should return a task that represents
    ///     the lifetime of the long running operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">
    ///     Triggered when
    ///     <see cref="Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is
    ///     called.
    /// </param>
    /// <returns>A <see cref="System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
    /// <remarks>
    ///     See <see href="https://docs.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for
    ///     implementation guidelines.
    /// </remarks>
    protected Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _longRunningTaskLifetime = new(stoppingToken);

            return _longRunningTaskLifetime.Task;
        }






    /// <summary>
    ///     Filters the found links and adds them to the download queue.
    /// </summary>
    /// <param name="foundLinks">The collection of scraped URLs.</param>
    /// <param name="page">The cached page.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FilterAndAddToQueueAsync(ConcurrentScrapedUrlCollection foundLinks, CachedPage page)
        {
            foreach (var s in foundLinks.Select(k => k.Key).Except(_downloadedLinks))
                {
                    try
                        {
                            await AddDownloadItemAsync(new(s, _options.OutputFilePath)).ConfigureAwait(false);
                        }
                    catch (SpyderException ex)
                        {
                            Log.AndContinue(ex);
                            //Remove the last entry that caused exception
                            _ = _cache.CacheIndexItems.Remove(page.Key, out _);
                        }
                }
        }






    /// <summary>
    ///     Reads the cached page source from disk based on the given page cache key.
    /// </summary>
    /// <param name="page">The key value pair of the page to read from cache.</param>
    /// <remarks>
    ///     <list type="table">
    ///         <item>
    ///             <term>key</term>
    ///             <description>The url of the page source.</description>
    ///         </item>
    ///         <item>
    ///             <term>value</term>
    ///             <description>The path to the cached page source.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <returns>The cached page source.</returns>
    private async Task<string> GetCachedPageSourceFromDiskAsync(CachedPage page)
        {
            // Combines the cache path and the page value to get the full path to the cached page.
            var pagePath = Path.Combine(_options.CacheLocation, page.Value);

            // Reads the cached page source from disk.
            var source = await File.ReadAllTextAsync(pagePath).ConfigureAwait(false);

            // Returns the cached page source.
            return source;
        }






    /// <summary>
    ///     Initializes the download controller. This method must be called before the controller can be used.
    /// </summary>
    private void Init()
        {
            // Log the fact that the controller is initializing
            _logger.SpyderInfoMessage("Download controller initializing");

            // Load the set of links that have already been downloaded from disk
            _downloadedLinks = LoadDownloadedLinksListFromDisk();

            // Load the list of pages that have already been searched from disk
            _searchedPages = LoadSearchedPagesFromDisk();

            // Log that the controller has finished initializing and is ready for use
            _logger.SpyderInfoMessage("Download controller initialized, searching for downloadable in cache");

            InitializationComplete?.Invoke(null, EventArgs.Empty);
        }






    /// <summary>
    ///     Loads the list of links that have already been downloaded from disk.
    /// </summary>
    private HashSet<string> LoadDownloadedLinksListFromDisk()
        {
            var path = Path.Combine(DOWNLOADED_PATH, "DownloadedLinks.txt");
            // If file doesn't exist create it and initialize the set with an empty set.
            if (!File.Exists(path))
                {
                    var x = File.CreateText(path);
                    x.Dispose();
                    return new();
                }

            lock (_readLock)
                {
                    // Read all lines from the file and create a new set with them.
                    var set = new HashSet<string>(File.ReadAllLines(path));

                    // Assign the set to the downloaded links field.
                    return set;
                }
        }






    /// <summary>
    ///     Loads the set of pages that have already been searched from disk.
    /// </summary>
    private HashSet<string> LoadSearchedPagesFromDisk()
        {
            var path = Path.Combine(DOWNLOADED_PATH, "SearchedPages.txt");

            // If file doesn't exist create it and initialize the set with an empty set.
            if (!File.Exists(path))
                {
                    var y = File.CreateText(path);
                    y.Dispose();

                    return new();
                }


            lock (_readLock)
                {
                    // Read all lines from the file and create a new set with them.
                    var set = new HashSet<string>(File.ReadAllLines(path));

                    // Assign the set to the searched pages field.
                    return set;
                }
        }

    #endregion
}



/// <summary>
///     Contains the information about a cached page.
/// </summary>
/// <remarks>
///     <list type="table">
///         <item>
///             <term>key</term>
///             <description>The url of the page source.</description>
///         </item>
///         <item>
///             <term>value</term>
///             <description>The path to the cached page source.</description>
///         </item>
///         <item>
///             <term>CachedPageInfo</term>
///             <description>The key value pair of the page to read from cache.</description>
///         </item>
///     </list>
/// </remarks>
public class CachedPage
{
    public CachedPage(KeyValuePair<string, string> info)
        {
            this.CachedPageInfo = info;
        }






    public CachedPage(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }






    public KeyValuePair<string, string> CachedPageInfo { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}