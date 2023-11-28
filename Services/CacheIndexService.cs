#region

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

using CommunityToolkit.Diagnostics;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : ICacheIndexService, IDisposable
{
    private static readonly object Locker = new();
    private static int s_cacheHits;
    private static int s_cacheMisses;
    private static readonly Mutex s_mutex = new();
    private readonly ISpyderClient _client;
    private readonly FileOperations _fileOperations;
    private readonly ILogger _logger;
    private readonly SpyderMetrics _metrics;
    private readonly SpyderOptions _options;
    private bool _disposed;
    private Stopwatch _timer;

    #region Properteez

    private ConcurrentDictionary<string, string> IndexCache { get; }

    #endregion

    #region Interface Members

    public ConcurrentDictionary<string, string> CacheIndexItems => this.IndexCache;

    /// <summary>
    ///     Cache Items currently in index
    /// </summary>
    public int CacheItemCount => this.IndexCache.Count;





    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }





    public async Task<string> GetAndSetContentFromCacheAsync(string address)
        {
            try
                {
                    _timer = new();
                    _timer.Start();

                    var content = await GetAndSetContentFromCacheCoreAsync(address: address)
                        .ConfigureAwait(false);

                    _timer.Stop();
                    if (_options.UseMetrics)
                        {
                            _metrics.CrawlElapsedTime(timing: _timer.ElapsedMilliseconds);
                        }

                    return content;
                }
            catch (SpyderException)
                {
                    _logger.SpyderWebException(message: "An error occured during a url retrieval.");

                    return string.Empty;
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
        IOptions<SpyderOptions> options,
        ISpyderClient client)
        {
            Guard.IsNotNull(value: options);
            _metrics = metrics;
            _logger = logger;
            _options = options.Value;
            _client = client;
            _fileOperations = new(options: _options);
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
            this.IndexCache = LoadCacheIndex();
            _logger.SpyderInfoMessage(message: "Cache Index Service Loaded...");
            _ = StartupComplete.TrySetResult(true);
        }





    public static int CacheHits => s_cacheHits;
    public static int CacheMisses => s_cacheMisses;
    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion

    #region Private Methods

    protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                {
                    if (disposing)
                        {
                            // unsubscribe from static event
                            SpyderControlService.LibraryHostShuttingDown -= OnStopping;
                        }

                    // Here you can release unmanaged resources if any
                    _fileOperations.Dispose();
                    _disposed = true;
                }
        }





    // Destructor
    ~CacheIndexService()
        {
            Dispose(false);
        }





    private static string GenerateUniqueCacheFilename(
        string cacheLocale)
        {
            var filename = Guid.NewGuid().ToString();
            _ = s_mutex.WaitOne();
            try
                {
                    while (File.Exists(Path.Combine(path1: cacheLocale, path2: filename)))
                        {
                            filename = Path.GetRandomFileName();
                        }

                    // create a file immediately to reserve the filename
                    using var k = File.Create(Path.Combine(path1: cacheLocale, path2: filename));
                }
            finally
                {
                    s_mutex.ReleaseMutex();
                }

            return filename;
        }





    private async Task<string> GetAndSetContentFromCacheCoreAsync([NotNull] string address)
        {
            return await TryGetCacheValue(key: address).ConfigureAwait(false);
        }





    private async Task GetCacheFileContents(string address, string filename, [Out] PageContent resultObj)
        {
            //Entry was found in cache
            if (_options.UseMetrics)
                {
                    _metrics.UrlCrawled(1, true);
                }

            _logger.SpyderDebug($"CACHE: Loading page {address}");
            _ = Interlocked.Increment(location: ref s_cacheHits);
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
                    catch (SpyderException)
                        {
                            _logger.InternalSpyderError(
                                message: "A critical error occured during cache entry retrieval.");
                        }
                }
            else
                {
                    _logger.InternalSpyderError(
                        message:
                        "A Cache entry was missing from disk. A cache index consistency check has been triggered. Checking cache consistency...");
                    VerifyCacheIndex();
                }
        }





    private async Task<string> GetValueAsyncInternal(string address)
        {
            // Not found in cache load from web
            _ = Interlocked.Increment(location: ref s_cacheMisses);
            if (_options.UseMetrics)
                {
                    _metrics.UrlCrawled(1, false);
                }

            _logger.SpyderTrace($"WEB: Loading page {address}");
            var content = await _client.GetContentFromWebWithRetryAsync(address: address)
                .ConfigureAwait(false);

            _ = await SetContentCacheAsync(content: content, address: address).ConfigureAwait(false);
            return content;
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
                    _logger.InternalSpyderError(message: "Failed to load cache index");


                    throw;
                }
            finally
                {
                    fileOperations.Dispose();
                }
        }





    private void OnStopping(object sender, EventArgs eventArgs)
        {
            try
                {
                    SaveCacheIndex();
                    _logger.SpyderInfoMessage(message: "Cache Index is saved");
                }
            catch (SpyderException)
                {
                    _logger.SpyderError(
                        message: "Error saving Cache Index. Consider checking data against backup file.");
                }
        }





    private string ReadCacheItemFromDisk(string keyVal)
        {
            return ReadFileContentsAsync(path: _options.CacheLocation, fileName: keyVal).Result;
        }





    private static Task<string> ReadFileContentsAsync(
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





    /// <summary>
    ///     Save page text to disk and add cache entry for address
    /// </summary>
    /// <param name="content"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    private async Task<PageContent> SetContentCacheAsync(string content, string address)
        {
            PageContent resultObj = new(new(uriString: address))
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
                    var filename = GenerateUniqueCacheFilename(cacheLocale: _options.CacheLocation);
                    resultObj.CacheFileName = filename;
                    await FileOperations.SafeFileWriteAsync(
                            Path.Combine(path1: _options.CacheLocation, path2: filename), contents: resultObj.Content)
                        .ConfigureAwait(false);

                    _ = this.IndexCache.TryAdd(key: address, value: filename);
                }
            catch (Exception e)
                {
                    _logger.InternalSpyderError(message: e.Message);
                    throw;
                }

            return resultObj;
        }





    private Task<string> TryGetCacheValue(string key)
        {
            return this.IndexCache.TryGetValue(key: key, out var keyVal)
                ? Task.FromResult(ReadCacheItemFromDisk(keyVal: keyVal))
                : GetValueAsyncInternal(address: key);
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
} //namespace