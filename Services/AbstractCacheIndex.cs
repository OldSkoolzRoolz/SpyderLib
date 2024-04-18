
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MySql.Data.MySqlClient;



namespace KC.Apps.SpyderLib.Services;




public abstract class AbstractCacheIndex : BackgroundService
{
    #region feeeldzzz

    internal  int _cacheHits;
    
    internal  int _cacheMisses;
    private static readonly Mutex s_mutex = new();
    private readonly IMyClient _client;
    internal readonly ILogger _logger;
    internal readonly SpyderMetrics _metrics;
    internal readonly SpyderOptions _options;
    internal static readonly ConcurrentBag<string> s_CachedUrls = new();
    //private readonly ScrapedUrls _scrapedUrls = new(SpyderControlService.Options.StartingUrl);

    #endregion






    private protected AbstractCacheIndex(
        [NotNull] IMyClient client,
        [NotNull] ILogger logger,
        [NotNull] SpyderMetrics metrics)
    {
        _options = AppContext.GetData("options") as SpyderOptions;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

        
    }






    #region Properteez

    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    #region Private Methods

    private static string GenerateUniqueCacheFilename(string cacheLocale)
    {
        var filename = Guid.NewGuid().ToString();

        _ = s_mutex.WaitOne();
        try
        {
            while (File.Exists(Path.Combine(cacheLocale, filename)))
            {
                filename = Path.GetRandomFileName();
            }

            // create a file immediately to reserve the filename
            using var k = File.Create(Path.Combine(cacheLocale, filename));
        }
        finally
        {
            s_mutex.ReleaseMutex();
        }

        return filename;
    }









    // Example connection string format:
    private const string CONNECTION_STRING = "server=localhost;user=plato;password=password;database=spyderlib;";






    protected async Task<PageContent> GetAndSetContentFromCacheCoreAsync([NotNull] string address)
    {

        var pageContent = await TryGetCacheValueAsync(address).ConfigureAwait(false);

        return pageContent;
    }






    protected static async Task InsertNewCacheItemDbCoreAsync(string address, string filename)
    {
        /*
        var policy = Polly.Policy
            .Handle<AuthenticationException>(ex => ex.InnerException is Win32Exception)
            .Or<MySqlException>(ex => ex.Message.Contains("timeout"))
            .Or<AuthenticationException>()
            .Retry((exception, attempt) =>
            {                        
             _logger.LogError(exception, "Class: {Class} | Method: {Method} | Failure executing query, on attempt number: {Attempt}", GetType().Name,
                    MethodBase.GetCurrentMethod().Name, attempt);
            });
        */
        try
        {
            var sql = "INSERT INTO cacheindex (address, filename) VALUES (@address, @filename)";
            await MySqlHelper.ExecuteNonQueryAsync(CONNECTION_STRING,sql,
                            new[] { new MySqlParameter("@address", address), new MySqlParameter("@filename", filename) }).ConfigureAwait(false);

        }
        catch (MySqlException e) when (e.Message.Contains("duplicate",StringComparison.OrdinalIgnoreCase))
        {
            Debugger.Break();
            Console.WriteLine(e.Message);
        }



    }








    protected static async Task<Collection<string>> GetCachedItemsAsync()
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            var query = "Select address from cacheindex";
            var reader = await MySqlDatabase.ExecuteMySqlReaderAsync(query,null).ConfigureAwait(false);

            Collection<string> urls = [];
            while ( await reader.ReadAsync().ConfigureAwait(false))
            {
                urls.Add(reader["address"].ToString());
            }
            return urls;




        }
        catch (Exception e)
        {
            Log.Debug(e.Message);
            return new();
        }
    }


















    private static string GetCacheFileNameFromDb(string address)
    {
        try
        {
     

            var sql = "SELECT filename FROM cacheindex WHERE address = @address";
            var filename = (string)MySqlHelper.ExecuteScalar(CONNECTION_STRING,sql, new MySqlParameter("@address", address));
            return filename;



        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return string.Empty;
    }






    private async Task<string> GetCacheFileContents(string address, string filename)
    {
        //Entry was found in cache
        if (_options.UseMetrics)
        {
            _metrics.UrlCrawled(1, true);
        }

        _logger.SpyderTrace($"CACHE HIT: Loading page {address}");
        var cacheEntryPath = Path.Combine(_options.CacheLocation, filename);

        if (File.Exists(cacheEntryPath))
        {
            try
            {
                return await File
                    .ReadAllTextAsync(Path.Combine(_options.CacheLocation, filename))
                    .ConfigureAwait(false);
            }
            catch (SpyderException)
            {
                _logger.InternalSpyderError(
                    "A critical error occured during cache entry retrieval.");

                return "error";
            }
        }
        else
        {
            _logger.InternalSpyderError(
                "A Cache entry was missing from disk. A cache index consistency check has been triggered. Checking cache consistency...");

            return "error";
        }
    }






    /// <summary>
    ///     Save page text to disk and add cache entry for address
    /// </summary>
    /// <param name="content"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    protected async Task<PageContent> SetContentCacheAsync(string content, string address)
    {
        PageContent resultObj = new(new(address))
        {
            FromCache = false,
            Content = content
        };
        if (content is null)
        {
            return resultObj;
        }

        try
        {
            //Generate a unique filename to save the entry to disk.
            var filename = GenerateUniqueCacheFilename(_options.CacheLocation);
            resultObj.CacheFileName = filename;
            await FileOperations.SafeFileWriteAsync(
                    Path.Combine(_options.CacheLocation, filename), resultObj.Content)
                .ConfigureAwait(false);

            await InsertNewCacheItemDbCoreAsync(address, filename).ConfigureAwait(false);
        }
        catch (Exception e) when (e.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            _logger.InternalSpyderError("Duplicate entry in cache. A cache index consistency check has been triggered. This should not happen.");

        }

        return resultObj;
    }






    private async Task<PageContent> TryGetCacheValueAsync(string address)
    {
        // Atttempt to get entry filename if it exists in cache db and gets value from the web if it doesn't
        var filename = GetCacheFileNameFromDb(address);
        PageContent page = new(new Uri(address));
        page.CacheFileName = filename;

        try
        {
            if (!string.IsNullOrEmpty(filename))
            {
                //Retrieve content from cache
                page.Content = await GetCacheFileContents(address, filename).ConfigureAwait(false);
                page.FromCache = true;
                Interlocked.Increment(ref _cacheHits);
            }
            else
            {
                //Not found in cache but we loaded from web.
                Interlocked.Increment(ref _cacheMisses);
                page.FromCache = false;
                page.Content = await _client.GetPageContentFromWebAsync(address).ConfigureAwait(false);
                //save to cache
                await SetContentCacheAsync(page.Content, address).ConfigureAwait(false);
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            page.Exception = e;
        }

        return page;

    }






    #endregion
}