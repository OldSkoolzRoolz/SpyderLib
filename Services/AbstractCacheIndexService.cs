using System.Collections.Concurrent;
using System.Runtime.InteropServices;

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

    protected static int s_cacheHits;
    protected static int s_cacheMisses;
    private static readonly object s_locker = new();
    private static readonly Mutex s_mutex = new();
    private readonly IMyClient _client;
    internal readonly ILogger _logger;
    internal readonly SpyderMetrics _metrics;
    internal readonly SpyderOptions _options;
    internal bool _disposed;

    #endregion






    private protected AbstractCacheIndex(
        [NotNull] IMyClient client,
        [NotNull] ILogger logger,
        [NotNull] SpyderMetrics metrics)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

        _options = AppContext.GetData("options") as SpyderOptions;
    }






    #region Properteez

    public static ConcurrentDictionary<string, string> IndexCache { get; set; }
    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    #region Private Methods

    private static string GenerateUniqueCacheFilename(string cacheLocale, Uri uri)
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






    private static MySqlConnection GetMySqlConnection(string connectionString)
    {
        return new(connectionString);
    }






    // Example connection string format:
    private static readonly string connectionString = "server=localhost;user=sa;password=password;database=spyderlib;";






    protected async Task<string> GetAndSetContentFromCacheCoreAsync([NotNull] string address)
    {
        return await TryGetCacheValue(address).ConfigureAwait(false);
    }






    private static void InsertNewCacheItem(string address, string filename)
    {
        try
        {
            using var conn = GetMySqlConnection(connectionString);
            conn.Open();

            var sql = "INSERT INTO CacheIndex (siteurl, filename) VALUES (@address, @filename)";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@address", address);
            cmd.Parameters.AddWithValue("@filename", filename);
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }






    private static string GetCacheFileNameFromDb(string address)
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);

            conn.Open();

            var sql = "SELECT filename FROM CacheIndex WHERE siteurl = @address";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@address", address);
            var filename = (string)cmd.ExecuteScalar();
            return filename;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }






    private async Task GetCacheFileContents(string address, string filename, [Out] PageContent resultObj)
    {
        //Entry was found in cache
        if (_options.UseMetrics)
        {
            _metrics.UrlCrawled(1, true);
        }

        _logger.SpyderTrace($"CACHE HIT: Loading page {address}");
        _ = Interlocked.Increment(ref s_cacheHits);
        var cacheEntryPath = Path.Combine(_options.CacheLocation, filename);
        resultObj.CacheFileName = filename;
        resultObj.FromCache = true;
        if (File.Exists(cacheEntryPath))
        {
            try
            {
                resultObj.Content = await File
                    .ReadAllTextAsync(Path.Combine(_options.CacheLocation, filename))
                    .ConfigureAwait(false);
            }
            catch (SpyderException)
            {
                _logger.InternalSpyderError(
                    "A critical error occured during cache entry retrieval.");
            }
        }
        else
        {
            _logger.InternalSpyderError(
                "A Cache entry was missing from disk. A cache index consistency check has been triggered. Checking cache consistency...");

        }
    }






    private async Task<string> GetValueAsyncInternal(string address)
    {
        // Not found in cache load from web
        _ = Interlocked.Increment(ref s_cacheMisses);
        if (_options.UseMetrics)
        {
            _metrics.UrlCrawled(1, false);
        }

        _logger.SpyderTrace($"Cache miss: Loading page {address}");
        var content = await _client.GetPageContentFromWebAsync(address)
            .ConfigureAwait(false);
        if (string.IsNullOrEmpty(content))
        {
            return "error";
        }

        _ = await SetContentCacheAsync(content, address).ConfigureAwait(false);
        return content;
    }











    private string ReadCacheItemFromDisk(string keyVal)
    {
        return ReadFileContentsAsync(_options.CacheLocation, keyVal).Result;
    }






    private static Task<string> ReadFileContentsAsync(
        string path,
        string fileName)
    {
        return Task.Run(() =>
            {
                var fullPath = Path.Combine(path, fileName);
                lock (s_locker)
                {
                    return File.ReadAllText(fullPath);
                }
            });
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
            var filename = GenerateUniqueCacheFilename(_options.CacheLocation, new(address));
            resultObj.CacheFileName = filename;
            await FileOperations.SafeFileWriteAsync(
                    Path.Combine(_options.CacheLocation, filename), resultObj.Content)
                .ConfigureAwait(false);

            InsertNewCacheItem(address, filename);
        }
        catch (Exception e)
        {
            _logger.InternalSpyderError(e.Message);
            throw;
        }

        return resultObj;
    }






    private async Task<string> TryGetCacheValue(string address)
    {
        // Atttempt to get value from cache DB if it exists or gets value from the web if it doesn't
        var filename = GetCacheFileNameFromDb(address);

        if (!string.IsNullOrEmpty(filename))
        {
            return await Task.FromResult(ReadCacheItemFromDisk(filename));
        }

        return await Task.FromResult(await GetValueAsyncInternal(address));


    }






    #endregion
}