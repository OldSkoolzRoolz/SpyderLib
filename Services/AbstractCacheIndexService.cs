using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public abstract class AbstractCacheIndex
{
    private readonly IMyClient _client;
    internal bool _disposed;
    internal readonly ILogger _logger;
    internal readonly SpyderMetrics _metrics;
    internal readonly SpyderOptions _options;
    protected static int s_cacheHits;
    protected static int s_cacheMisses;
    private static readonly object s_locker = new();
    private static readonly Mutex s_mutex = new();






    private protected AbstractCacheIndex(
        [JetBrains.Annotations.NotNull] IMyClient client,
        [JetBrains.Annotations.NotNull] ILogger logger,
        [JetBrains.Annotations.NotNull] SpyderMetrics metrics)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

            _options = AppContext.GetData("options") as SpyderOptions;


            SpyderControlService.LibraryHostShuttingDown += OnStopping;
            IndexCache = LoadCacheIndex();
        }






    #region Properteez

    public static ConcurrentDictionary<string, string> IndexCache { get; private set; }
    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    #region Public Methods

    /// <summary>
    ///     Save Index to disk
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public void SaveCacheIndex()
        {
            using var fileOperations = new FileOperations();
            fileOperations.SaveCacheIndex(IndexCache);
        }

    #endregion






    #region Private Methods

    private static string GenerateUniqueCacheFilename(
        string cacheLocale)
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






    protected async Task<string> GetAndSetContentFromCacheCoreAsync([JetBrains.Annotations.NotNull] string address)
        {
            return await TryGetCacheValue(address).ConfigureAwait(false);
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
                    VerifyCacheIndex();
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
            if (string.IsNullOrEmpty(content)){return "error";}
            
            _ = await SetContentCacheAsync(content, address).ConfigureAwait(false);
            return content;
        }






    protected ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            try
                {
            using var fileOperations = new FileOperations();
                    // load the cache asynchronously to avoid blocking the constructor
                    return fileOperations.LoadCacheIndex();
                }
            catch (Exception)
                {
                    _logger.InternalSpyderError("Failed to load cache index");


                    throw;
                }
        }






    protected void OnStopping(object sender, EventArgs eventArgs)
        {
            try
                {
                    SaveCacheIndex();
                    _logger.SpyderInfoMessage("Cache Index is saved");
                }
            catch (SpyderException)
                {
                    _logger.SpyderError(
                        "Error saving Cache Index. Consider checking data against backup file.");
                }
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
                    var filename = GenerateUniqueCacheFilename(_options.CacheLocation);
                    resultObj.CacheFileName = filename;
                    await FileOperations.SafeFileWriteAsync(
                            Path.Combine(_options.CacheLocation, filename), resultObj.Content)
                        .ConfigureAwait(false);

                    _ = IndexCache.TryAdd(address, filename);
                }
            catch (Exception e)
                {
                    _logger.InternalSpyderError(e.Message);
                    throw;
                }

            return resultObj;
        }






    private Task<string> TryGetCacheValue(string key)
        {
            // Atttempt to get value from cache if it exists or gets value from the web if it doesn't
            return IndexCache.TryGetValue(key, out var keyVal)
                ? Task.FromResult(ReadCacheItemFromDisk(keyVal))
                : GetValueAsyncInternal(key);
        }






    /// <summary>
    ///     Verifies the cache index by performing cleaning of non-existing files and entries.
    /// </summary>
    private void VerifyCacheIndex()
        {
            // Create copies so we don't iterate live data.
            var cacheEntriesSnapshot = new Dictionary<string, string>(IndexCache);
            var directoryFilesSnapshot = Directory.GetFiles(_options.CacheLocation);

            var deletedFilesCount = 0;
            var deletedEntriesCount =
                (from item in cacheEntriesSnapshot
                    let entryFilePath = Path.Combine(_options.CacheLocation, item.Value)
                    where !File.Exists(entryFilePath)
                    select item).Count(item => IndexCache.Remove(item.Key, out _));


            SaveCacheIndex();
            // Iterate through a copy of the cache index and deletes entries if files don't exist.

            // Compare existing files in directory with cache entries.
            var currentCacheFileNames =
                new HashSet<string>(IndexCache.Values.Select(filename =>
                    Path.Combine(_options.CacheLocation,
                        filename)));
            var redundantFiles = directoryFilesSnapshot.Except(currentCacheFileNames);

            // Delete files that are missing in the cache.
            foreach (var file in redundantFiles)
                {
                    try
                        {
                            File.Delete(file);
                            deletedFilesCount++;
                        }
                    catch (SpyderException)
                        {
                            _logger.InternalSpyderError(
                                $"Failed to delete file: {file}. It can be either used by another process or doesn't exist anymore.");
                        }
                }

            SaveCacheIndex();
            _logger.SpyderInfoMessage(
                $"Cache verification complete. {deletedFilesCount} files and {deletedEntriesCount} entries deleted.");
        }

    #endregion
}