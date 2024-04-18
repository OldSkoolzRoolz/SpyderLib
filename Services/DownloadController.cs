using System.Diagnostics;
using System.Globalization;

using CommunityToolkit.Diagnostics;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;

using MySql.Data.MySqlClient;



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


    private const string CONNECTION_STRING =
        "server=localhost;user=plato;password=password;database=spyderlib;";

    [NotNull] private readonly ICacheIndexService _cache;
    [NotNull] private readonly IBackgroundDownloadQue _downloadQue;
    [NotNull] private readonly ILogger _logger;
    [NotNull] private readonly SpyderOptions _options;
    private readonly HashSet<string> _downloadedLinks = new();
    private TaskCompletionSource _longRunningTaskLifetime;

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

        InitializationComplete += OnInitComplete;
        DownloadQueueLoadComplete += OnQueueLoadComplete;
        DownloadsCompleted += OnDownloadsComplete;


        var fact = (LoggerFactory)AppContext.GetData("factory");
        Guard.IsNotNull(fact);
        _logger = fact.CreateLogger<DownloadController>();

        _options = AppContext.GetData("options") as SpyderOptions ??
                   throw new ArgumentException("_options");

        _downloadQue = downloadQue ?? throw new ArgumentNullException(nameof(downloadQue));
        _cache = cacheIndexService ?? throw new ArgumentNullException(nameof(cacheIndexService));

        Init();

    }






    #region Properteez

    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    private void OnDownloadsComplete(object sender, EventArgs e)
    {
        Console.WriteLine("***   Downloads Completed *****");
    }






    private void OnQueueLoadComplete(object sender, EventArgs e)
    {
        _logger.LogInformation("Download Queue Load Complete Tasks Available == {0}",
            _downloadQue.Count.ToString(CultureInfo.InvariantCulture));


        _downloadQue.PostingComplete();

    }






    private void OnInitComplete(object sender, EventArgs e)
    {
        _ = StartupComplete.TrySetResult(true);
    }






    #region Public Methods

    /// <summary>
    ///     <b>avail - pages that have not been searched</b>
    ///     Searches source code of 200 pages in local cache for the htmlElement specified in SpyderOptions
    ///     and creates a downloadItem task to the downloadQue
    /// </summary>
    public async Task LoadProducerQueueAsync()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //Gets all the pages that have not been searched for tags from DB
        Dictionary<string, string> unsearchedCache = await GetCacheItemsSearchedAsync().ConfigureAwait(false);


        // if there are no pages to search then fire the event that the download queue has finished loading
        if (unsearchedCache.Count == 0)
        {
            DownloadQueueLoadComplete?.Invoke(null, EventArgs.Empty);
            return;
        }

        Console.WriteLine("Searching for tags...\n");
        // Creates a task for each page to search for tags

        Parallel.ForEach(unsearchedCache, async t => await SearchCachedPageAsync(t).ConfigureAwait(false));


        stopwatch.Stop();

        // ReSharper disable once LocalizableElement
        Console.WriteLine($"Download Queue loaded Elapsed time:          {stopwatch.Elapsed}\n");

        // fires the event that the download queue has finished loading
        DownloadQueueLoadComplete?.Invoke(null, EventArgs.Empty);
    }






    /// <summary>
    ///     Gets all the pages from the local cache that have not been searched for tags
    /// </summary>
    /// <returns>A list of urls ofpages that have not been searched for tags</returns>
    private async static Task<Dictionary<string, string>> GetCacheItemsSearchedAsync()
    {
        // Opens a connection to the local cache
        using var conn = new MySqlConnection(CONNECTION_STRING);
        await conn.OpenAsync().ConfigureAwait(false);

        // SQL command to get all the pages from the local cache that have not been searched
        var sql = "SELECT siteurl, filename FROM CacheIndex where searched = false limit 1000";

        // Sends the SQL command to the local cache
        using var cmd = new MySqlCommand(sql, conn);
        var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        // List to hold all the pages from the local cache that have not been searched
        var urls = new Dictionary<string, string>();

        // Loops through each page from the local cache
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            // Adds the page to the list of pages that have not been searched
            urls.Add(reader["siteurl"].ToString(), reader["filename"].ToString());
        }

        return urls;
    }












    private async static Task<List<string>> GetDownloadedItemsDbAsync()
    {

       try
       {
         using var conn = new MySqlConnection(CONNECTION_STRING);
         await conn.OpenAsync().ConfigureAwait(false);
 
         var sql = "SELECT address FROM downloadedurls where downloaded = 1";
         using var cmd = new MySqlCommand(sql, conn);
         var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
         List<string> urls = [];
 
         while (await reader.ReadAsync().ConfigureAwait(false))
         {
             urls.Add(reader["siteurl"].ToString());
         }
 
         return urls;
       }
       catch (System.Exception)
       {
        
        Log.Debug("MySql Error in GetDownloadedItemsDBAsync");
        return new List<string>();
       }
    }









    private static async Task SetItemSearchedAsync(string address)
        {
            try
                {
                    var sql = "UPDATE cacheindex SET searched = 1 WHERE address= @address";
                    await MySqlDatabase.ExecuteNonQueryAsync(sql, new MySqlParameter("@address", address)).ConfigureAwait(false);


                }
            catch (System.IO.IOException ex)
                {
                    Log.Debug($"Error in SetItemSearchedAsync: {ex.Message}");
                }
            catch (MySqlException)
                {
                    Log.Debug("MySql Exception in SetItemSearchedAsync");
                }
                catch (Exception e) when (e.Message.Contains("timeout"))
                {
                Log.Debug("MySqlTimeout");
                }
        }











    private static string GetCacheFileNameFromDb(string address)
    {
        try
        {
            using var conn = new MySqlConnection(CONNECTION_STRING);

            conn.Open();

            var sql = "SELECT filename FROM CacheIndex WHERE siteurl = @address";
            var cmd = new MySqlCommand(sql, conn);
            _ = cmd.Parameters.AddWithValue("@address", address);
            var filename = (string)cmd.ExecuteScalar();
            cmd.Dispose();
            return filename;
        }
        catch (Exception e)
        {
            Log.Debug(e.Message+"MySql Exception in GetCacheFileNameFromDb");
            return string.Empty;
        }
    }






    /// <summary>
    ///     Processes a cached page from disk
    /// </summary>
    /// <param name="pageurl">The url of the cached page to process.</param>
    /// <returns>A <see cref="CachedPage" /> instance.</returns>
    private async Task SearchCachedPageAsync(KeyValuePair<string, string> item)
    {
        Console.WriteLine($"Searching {item.Key}...");

        await using var fileStream = (File.OpenRead(Path.Combine(_options.CacheLocation, item.Value).ToString()) ?? throw new FileNotFoundException(item.Value)).ConfigureAwait(false);
        using var sr = new StreamReader(fileStream);
        var pageSource = await sr.ReadToEndAsync().ConfigureAwait(false);

        await SetItemSearchedAsync(item.Key).ConfigureAwait(false);

        if (pageSource.Length <= 300)
        {
            return;
        }

        var foundLinks = HtmlParser.TryExtractUserTagFromDocument(HtmlParser.CreateHtmlDocument(pageSource), _options.HtmlTagToSearchFor);
        if (foundLinks.Count <= 0)
        {
            
            return;
        }

        Console.WriteLine($"Found {foundLinks.Count} links");

        await AddTagHitsToQueueAsync(foundLinks).ConfigureAwait(false);

        _ = foundLinks.AllUrlz.Select(k => AddToDownloadedUrlsDbAsync(k));

    }







    public void Start()
    {
        LoadProducerQueueAsync().Wait();
    }

    #endregion






    #region Private Methods

    internal async Task AddDownloadItemAsync(DownloadItem item)
    {
        await _downloadQue.QueueBackgroundWorkItemAsync(item).ConfigureAwait(false);

    }





    private static async Task AddToDownloadedUrlsDbAsync(string pageurl)
    {
        try
        {
            using var conn = new MySqlConnection(CONNECTION_STRING);
            await conn.OpenAsync().ConfigureAwait(false);
            var sql = "INSERT INTO downloadedurls (siteurl,downloaded) VALUES (@address,0)";
            using var cmd = new MySqlCommand(sql, conn);
            _ = cmd.Parameters.AddWithValue("@address", pageurl);
            _ = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (System.Exception)
        {

            Log.Debug("Unable to add to DB");
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
    private async Task AddTagHitsToQueueAsync(ScrapedUrls foundLinks)
    {
        foreach (var s in foundLinks.AllUrlz.Select(k => k).Except(_downloadedLinks))
        {
            try
            {
                await AddDownloadItemAsync(new(s, _options.OutputFilePath)).ConfigureAwait(false);
            }
            catch (SpyderException ex)
            {
                Log.AndContinue(ex);
            }
        }
        Console.WriteLine($"Added " + foundLinks.AllUrlz.Count() + " links to queue");
    }






    /// <summary>
    ///     Initializes the download controller. This method must be called before the controller can be used.
    /// </summary>
    private void Init()
    {
        // Log the fact that the controller is initializing
        _logger.SpyderInfoMessage("Download controller initializing");

        // Load the set of links that have already been downloaded to disk
        GetDownloadedItemsDbAsync().GetAwaiter().GetResult().ForEach(x => _downloadedLinks.Add(x));

        // Load the list of pages that have already been searched from disk

        // Log that the controller has finished initializing and is ready for use
        _logger.SpyderInfoMessage("Download controller initialized, searching for downloadable in cache");

        InitializationComplete?.Invoke(null, EventArgs.Empty);
    }











    /// <summary>
    ///     Loads the set of pages that have already been searched from disk.
    /// </summary>
    private static HashSet<string> LoadSearchedPagesFromDb()
    {
       try
       {
         using var conn = new MySqlConnection(CONNECTION_STRING);
         conn.Open();
         var sql = "SELECT address FROM cacheindex where searched = 1";
         using var cmd = new MySqlCommand(sql, conn);
         
         var reader = cmd.ExecuteReader();
         var set = new HashSet<string>();
         while (reader.Read())
         {
             set.Add(reader["siteurl"].ToString());
         }
 
         return set;
       }
       catch (System.Exception)
       {
        
        Log.Debug("Mysql Exception in LoadSearchedPagesFromDB");
        return new HashSet<string>();
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
