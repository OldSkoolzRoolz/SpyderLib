#region

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : ICacheIndexService
{
    private static int s_cacheHits;
    private static int s_cacheMisses;
    private static readonly Mutex s_mutex = new();
    private readonly ISpyderClient _client;
    private readonly FileOperations _fileOperations;
    private readonly ILogger _logger;
    private readonly SpyderMetrics _metrics;
    private readonly SpyderOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Stopwatch _timer;

    #region Properteez

    private ConcurrentDictionary<string, string> IndexCache { get; }

    #endregion

    #region Interface Members

    /// <summary>
    ///     Cache Items currently in index
    /// </summary>
    public int CacheItemCount => this.IndexCache.Count;





    /// <summary>
    ///     Method gets page data from web or local cache
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public async Task<PageContent> GetAndSetContentFromCacheAsync(
        string address)
        {
            try
                {
                    if (_options.UseMetrics)
                        {
                            _timer = new();
                            _timer.Start();
                        }

                    var content = await GetAndSetContentFromCacheCoreAsync(address: address).ConfigureAwait(false);
                    if (_options.UseMetrics)
                        {
                            _timer.Stop();
                            _metrics.CrawlElapsedTime(timing: _timer.ElapsedMilliseconds);
                        }

                    return content;
                }
            catch (Exception e)
                {
                    _logger.SpyderWebException(message: "An error occured during a url retrieval.");
                    PageContent pc = new(url: address)
                        {
                            Exception = e
                        };
                    return pc;
                }
        }





    /// <summary>
    ///     Save Index to disk
    /// </summary>
    public void SaveCacheIndex()
        {
            using var fileOperations = new FileOperations(options: _options);
            fileOperations.SaveCacheIndex(options: _options, concurrentDictionary: this.IndexCache);
        }





    public Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address)
        {
            return SetContentCacheAsync(content: content, address: address);
        }

    #endregion

    #region Public Methods

    public CacheIndexService(SpyderMetrics metrics,
        ILogger<CacheIndexService> logger,
        IOptions<SpyderOptions> options)
        {
            _metrics = metrics;
            _logger = logger;
            _options = options.Value;
            _client = new SpyderClient(logger: _logger);
            _fileOperations = new(options: _options);
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
            this.IndexCache = LoadCacheIndex();
            Log.Information(message: "Cache Index Service Loaded...");
            StartupComplete.TrySetResult(true);
        }





    public static int CacheHits => s_cacheHits;
    public static int CacheMisses => s_cacheMisses;
    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion

    #region Private Methods

    private static string GenerateUniqueCacheFilename(
        string cacheLocale)
        {
            var filename = Guid.NewGuid().ToString();
            s_mutex.WaitOne();
            try
                {
                    while (File.Exists(Path.Combine(path1: cacheLocale, path2: filename)))
                        {
                            filename = Path.GetRandomFileName();
                        }

                    // create a file immediately to reserve the filename
                    File.Create(Path.Combine(path1: cacheLocale, path2: filename));
                }
            finally
                {
                    s_mutex.ReleaseMutex();
                }

            return filename;
        }





    private async Task<PageContent> GetAndSetContentFromCacheCoreAsync([NotNull] string address)
        {
            PageContent resultObj = new(url: address);
            string filename = null;
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
                {
                    if (!this.IndexCache.TryGetValue(key: address, value: out filename))
                        {
                            Interlocked.Increment(location: ref s_cacheMisses);
                            if (_options.UseMetrics)
                                {
                                    _metrics.UrlCrawled(1, false);
                                }

                            _logger.LogTrace(message: "WEB: Loading page {0}", address);
                            var content = await _client.GetContentFromWebWithRetryAsync(address: address)
                                .ConfigureAwait(false);
                            if (content.StartsWith(value: "Error:", comparisonType: StringComparison.Ordinal))
                                {
                                    resultObj.Exception = new SpyderException(message: content);
                                    resultObj.Content = content;
                                    return resultObj;
                                }

                            return await SetContentCacheAsync(content: content, address: address).ConfigureAwait(false);
                        }
                }
            finally
                {
                    _semaphore.Release();
                }

            await GetCacheFileContents(address: address, filename: filename, resultObj: resultObj)
                .ConfigureAwait(false);

            return resultObj;
        }





    private async Task GetCacheFileContents(string address, string filename, [Out] PageContent resultObj)
        {
            //Entry was found in cache
            if (_options.UseMetrics)
                {
                    _metrics.UrlCrawled(1, true);
                }

            _logger.LogDebug(message: "CACHE: Loading page {0}", address);
            Interlocked.Increment(location: ref s_cacheHits);
            var cacheEntryPath = Path.Combine(path1: _options.CacheLocation, path2: filename);
            resultObj.CacheFileName = filename;
            resultObj.FromCache = true;
            if (File.Exists(path: cacheEntryPath))
                {
                    try
                        {
                            resultObj.Content = await File
                                .ReadAllTextAsync(Path.Combine(path1: _options.CacheLocation, path2: filename))
                                .ConfigureAwait(false);
                        }
                    catch (Exception e)
                        {
                            resultObj.Exception = e;
                            _logger.LogCritical(message: "A critical error occured during cache entry retrieval.");
                        }
                }
            else
                {
                    _logger.LogCritical(
                        message:
                        "A Cache entry was missing from disk. A cache index consistency check has been triggered. Checking cache consistency...");
                    VerifyCacheIndex();
                }
        }





    private ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            var fileOperations = new FileOperations(options: _options);
            try
                {
                    // load the cache asynchronously to avoid blocking the constructor
                    return fileOperations.LoadCacheIndex();
                }
            catch (Exception)
                {
                    _logger.LogError(message: "Failed to load cache index");


                    throw;
                }
        }





    private void OnStopping(object sender, EventArgs eventArgs)
        {
            Log.Trace(message: "Cache service is shutting down...");

            try
                {
                    SaveCacheIndex();
                    Log.Information(message: "Cache Index is saved");
                }
            catch (Exception)
                {
                    Log.Error(message: "Error saving Cache Index");
                }
        }





    /// <summary>
    ///     Save page text to disk and add cache entry for address
    /// </summary>
    /// <param name="content"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    private async Task<PageContent> SetContentCacheAsync(string content, string address)
        {
            PageContent resultObj = new(url: address)
                {
                    FromCache = false,
                    Content = content
                };
            try
                {
                    //Generate a unique filename to save the entry to disk.
                    var filename = GenerateUniqueCacheFilename(cacheLocale: _options.CacheLocation);
                    resultObj.CacheFileName = filename;
                    await _fileOperations.SafeFileWriteAsync(
                            Path.Combine(path1: _options.CacheLocation, path2: filename), contents: resultObj.Content)
                        .ConfigureAwait(false);
                  
                            this.IndexCache.TryAdd(key: address, value: filename);
                }
            catch (Exception e)
                {
                    _logger.LogError(message: e.Message, e);
                    throw;
                }

            return resultObj;
        }





    /// <summary>
    ///     Verifies the cache index by performing cleaning of non-existing files and entries.
    /// </summary>
    private void VerifyCacheIndex()
        {
            // Create copies so we don't iterate live data.
            var cacheEntriesSnapshot = new Dictionary<string, string>(dictionary: this.IndexCache);
            var directoryFilesSnapshot = Directory.GetFiles(path: _options.CacheLocation);

            var deletedFilesCount = 0;
            var deletedEntriesCount =
                (from item in cacheEntriesSnapshot
                    let entryFilePath = Path.Combine(path1: _options.CacheLocation, path2: item.Value)
                    where !File.Exists(path: entryFilePath)
                    select item).Count(item => this.IndexCache.Remove(key: item.Key, value: out _));

            
            SaveCacheIndex();
            // Iterate through a copy of the cache index and deletes entries if files don't exist.

            // Compare existing files in directory with cache entries.
            var currentCacheFileNames =
                new HashSet<string>(this.IndexCache.Values.Select(filename =>
                    Path.Combine(path1: _options.CacheLocation,
                        path2: filename)));
            var redundantFiles = directoryFilesSnapshot.Except(second: currentCacheFileNames);

            // Delete files that are missing in the cache.
            foreach (var file in redundantFiles)
                {
                    try
                        {
                            File.Delete(path: file);
                            deletedFilesCount++;
                        }
                    catch (Exception e)
                        {
                            _logger.LogWarning(exception: e,
                                $"Failed to delete file: {file}. It can be either used by another process or doesn't exist anymore.");
                        }
                }

            SaveCacheIndex();
            _logger.LogInformation(
                $"Cache verification complete. {deletedFilesCount} files and {deletedEntriesCount} entries deleted.");
        }

    #endregion
}

public class CacheFileHandlerService
{
    private static readonly object Locker = new();

    #region Public Methods

    public Task<string> ReadFileContentsAsync(
        string path,
        string fileName)
        {
            return Task.Run(() =>
                {
                    var fullPath = Path.Combine(path1: path, path2: fileName);
                    lock (Locker)
                        {
                            return File.ReadAllText(path: fullPath);
                        }
                });
        }

    #endregion
} //namespace