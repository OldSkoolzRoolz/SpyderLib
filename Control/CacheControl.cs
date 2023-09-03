#region

using System.Collections.Concurrent;
using System.Net;

using KC.Apps.SpyderLib.Logging;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

#endregion

namespace KC.Apps.SpyderLib.Control;

public interface ICacheControl
{
    Task? ExecuteTask { get; }





    /// <summary>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    bool AddContentToCache(string address, string content);





    void Dispose();


    string GenerateUniqueFilename();





    /// <summary>
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    Task<string?> GetWebPageSourceAsync(string address);





    ConcurrentDictionary<string, string> LoadCacheIndex();


    bool SafeFileWrite(string path, string contents);
}

public class CacheControl
{
    public static event Action CacheShuttingDown;
    private const string FILENAME = "Spyder_Cache_Index.json";
    private readonly IMemoryCache _cache;
    private object _fileLock;
    private bool _isCacheInitialized;
    private object _lock = new();
    private bool _lockTaken;
    private readonly ILogger _logger;
    private readonly KC.Apps.SpyderLib.Properties.SpyderOptions _options;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1);
    private static readonly HttpClient HttpClientInstance = new() { Timeout = Timeout.InfiniteTimeSpan };





    public CacheControl(ILoggerFactory factory, IOptions<KC.Apps.SpyderLib.Properties.SpyderOptions> options,
        IMemoryCache                   cache)
    {
        _cache = cache;

        _logger = factory.CreateLogger<CacheControl>();
        _options = options.Value;
    }





    //######################################################################################################3
    //******************************************************************************************************





    /// <summary>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public bool AddContentToCache(string address, string content)
    {
        var filename = GenerateUniqueFilename();
        if (string.IsNullOrEmpty(value: filename))
        {
            _logger.LogCritical(message: "Getting Unique filename failed");
        }

        var path = Path.Combine(path1: _options.CacheLocation, path2: filename);
        if (SafeFileWrite(path: path, contents: content))
        {
            var cacheadded = AddToCache(uniqueUrlKey: address, cacheFileName: filename);
            SaveCacheIndex();
            return cacheadded;
        }

        _logger.LogError(message: "Failed to save cache file or db entry");
        return false;
    }





    private bool AddToCache(string uniqueUrlKey, string cacheFileName)
    {
        MemoryCacheOptions coptions = new()
                                      {
                                          TrackStatistics = true,
                                          ExpirationScanFrequency = TimeSpan.FromMinutes(60)
                                      };
        MemoryCacheEntryOptions options = new()
                                          {
                                              SlidingExpiration = TimeSpan.FromDays(1)
                                          };
        _ = options.RegisterPostEvictionCallback(callback: OnPostEviction);
        _cache.Set(key: uniqueUrlKey, value: cacheFileName, options: options);

        return true;
    }





    private void DeleteFileInCache(string file)
    {
        var filePath = Path.Combine(path1: _options.CacheLocation, path2: file);
        if (File.Exists(path: filePath))
        {
            File.Delete(path: filePath);
        }
    }





    public string GenerateUniqueFilename()
    {
        string filename;
        do
        {
            filename = Path.GetRandomFileName();
        } while (File.Exists(Path.Combine(path1: _options.CacheLocation, path2: filename)));

        return filename;
    }





    private IEnumerable<string> GetExistingCacheFiles()
    {
        return Directory
               .EnumerateFiles(path: _options.CacheLocation)
               .Select(selector: Path.GetFileName);
    }





    /// <summary>
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public async Task<string> GetHttpContentFromWebAsync(string address)
    {
        var content = string.Empty;
        try
        {
            var resp = await HttpClientInstance.GetAsync(requestUri: address,
                                                         completionOption: HttpCompletionOption.ResponseContentRead);

            // if success read content
            if (resp.IsSuccessStatusCode)
            {
                content = await resp.Content.ReadAsStringAsync();
            }

            // If unsuccessful try to recover and retry
            switch (resp.StatusCode)
            {
                case HttpStatusCode.Found:
                {
                    var str = await GetHttpContentFromWebAsync(resp.Headers.Location.ToString());
                    break;
                }
                case HttpStatusCode.Unauthorized:
                {
                    //They didn't like us poking around so we will just log and carry on.
                    _logger.PageCacheException("Unauthorized web response. Moving on.");
                    break;
                }
                case HttpStatusCode.Forbidden:
                {
                    //we get the boot again. Rinse and repeat....
                    _logger.PageCacheException("Unauthorized web response. Moving on.");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            // Errors here should be avoided at all costs.
            _logger.UnexpectedResultsException(message: e.Message);
        }

        return content;
    }





    /// <summary>
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public async Task<string?> GetWebPageSourceAsync(string address)
    {
        if (!_options.UseLocalCache)
        {
            return await GetHttpContentFromWebAsync(address: address);
        }

        // Attempt to get page from local cache.
        var cache = TryGetFileNameFromCache(uniqueUrlKey: address);
        if (!string.IsNullOrEmpty(value: cache))
        {
            _logger.DebugTestingMessage(message: "Page loaded from cache");
            return cache;
        }

        // Cache failed to load or doesn't exist so we Get from web
        cache = await GetHttpContentFromWebAsync(address: address);
        if (string.IsNullOrEmpty(value: cache))
        {
            _logger.DebugTestingMessage(message: "Failed to get page from cache or web.");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(value: cache))
        {
            // Add page to cache
            AddContentToCache(address: address, content: cache);
        }

        return cache;
    }





    private void InitializeCache()
    {
        _logger.LogInformation(message: "Loading cache.");

        try
        {
            var diskcache = LoadCacheIndex();

            if (!diskcache.IsEmpty && diskcache is not null)
            {
                _logger.LogInformation($"disccache count = {diskcache.Count}");
                foreach (var item in diskcache)
                {
                    AddToCache(uniqueUrlKey: item.Key, cacheFileName: item.Value);
                }

                var stats = _cache.GetCurrentStatistics();
                _logger.LogInformation(
                                       message: "Cache loaded from disk record count={count}",
                                       stats?.CurrentEntryCount);
            }
            else
            {
                _logger.LogWarning(
                                   message: "Unable to fetch records from disk to update cache.");
            }
        }
        finally
        {
            if (!_isCacheInitialized)
            {
                // _cacheSignal.Release();
                _isCacheInitialized = true;
            }
        }
    }





    /*
            public string? TryGetDocumentFromCache(string address)
            {
                if (!_pageCacheInitializationComplete) throw new PageCacheException("Cache not initilized");

                if (_cachedDownloads.TryGetValue(key: address, out string? cacheFileName))
                {
                    var p = $"{_options.CacheLocation}/{cacheFileName}";
                    var cachedContent = File.ReadAllText(p);
                    _logger.DebugTestingMessage(message: "Loaded From Cache");
                    return cachedContent;
                }

                return null;
            }

    */





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
    protected async Task InitializeModule(CancellationToken stoppingToken)
    {
        _logger.DebugTestingMessage(message: "Cache Control is starting");
        if (!_isCacheInitialized)
        {
            InitializeCache();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(message: "Will attempt to refresh the cache in {Minutes} minutes from now.",
                                       _updateInterval.Minutes);

                var stats = _cache.GetCurrentStatistics();
                _logger.LogInformation(message: "Cache hits {hits}", stats?.TotalHits);
                _logger.LogInformation(message: "Cache misses {miss}", stats?.TotalMisses);
                _logger.LogInformation(message: "Cache size {size}", stats?.CurrentEstimatedSize);
                await Task.Delay(delay: _updateInterval, cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.GeneralSpyderMessage(message: "Cancellation acknowledged: shutting down.");
                CacheShuttingDown?.Invoke();

                break;
            }
            finally
            {
                SaveCacheIndex();
                _logger.DebugTestingMessage(message: "Saving cache");
            }
        }
    }





    /*
            public void InitializeCache(ILogger logger, SpyderOptions options)
            {
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                if (_lock == null)
                {
                    _logger.PageCacheException("Lock is unintialized");
                }

                if (Monitor.TryEnter(_lock))
                {
                    try
                    {
                        if (_cachedDownloads is null)
                        {
                            var cacheIndex = LoadCacheIndex();
                            if (cacheIndex == null)
                            {
                                throw new Exception("Cache index is not loaded correctly.");
                            }

                            _cachedDownloads = cacheIndex;
                            VerifyCache();
                            _pageCacheInitializationComplete = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Add appropriate exception handling here.
                        _logger.PageCacheException(ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(_lock);
                    }
                }
                else
                {
                    _logger.PageCacheException("Could not acquire lock to initialize the cache.");
                }
            }

    */





    public ConcurrentDictionary<string, string> LoadCacheIndex()
    {
        var path = Path.Combine(path1: _options.OutputFilePath, path2: FILENAME);
        if (!File.Exists(path: path))
        {
            return new();
        }

        try
        {
            lock (_fileLock)
            {
                var json = File.ReadAllText(path: path);
                var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(value: json);
                return dict ?? new();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(exception: e, message: "Exception occured when loading cached index");
            throw;
        }
    }





    private void OnPostEviction(object key, object? value, EvictionReason reason, object state)
    {
        Console.WriteLine($"value = {value} reason is {reason}");
    }





    /*
            private void RemoveIndicesWithNoFilesOnDisk()
            {
                if (string.IsNullOrEmpty(_options?.CacheLocation))
                    throw new ArgumentException("CacheLocation cannot be null or empty");

                var indicesToRemove = _cachedDownloads?.Where(index =>
                                          !File.Exists(Path.Combine(_options.CacheLocation, index.Value))).ToList() ??
                                      new List<KeyValuePair<string, string>>();

                foreach (var itm in indicesToRemove)
                {
                    // Assuming _cachedDownloads is a dictionary
                    _cachedDownloads?.TryRemove(itm);
                }
            }

    */
    /*


            private void RemoveUnindexedFiles(IEnumerable<string> existingFiles)
            {
                var filesToRemove = existingFiles.Except(_cachedDownloads.Values);
                foreach (var file in filesToRemove.Where(file => !string.IsNullOrEmpty(file)))
                {
                    DeleteFileInCache(file);
                }
            }
    */





    public bool SafeFileWrite(string path, string contents)
    {
        try
        {
            lock (_fileLock)
            {
                File.WriteAllText(path: path, contents: contents);
                if (!File.Exists(path: path))
                {
                    Console.WriteLine(value: "failed to save file");
                    return false;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(exception: e, message: "Exception occured when writing file");
            return false;
        }
    }





    internal void SafeSerializeAndWrite(string path, string oldpath, string backpath)
    {
        lock (_fileLock)
        {
            var json     = JsonConvert.SerializeObject(value: _cache, formatting: Formatting.Indented);
            var origpath = Path.Combine(path1: _options.OutputFilePath, path2: FILENAME);
            var newfile  = Path.Combine(path1: _options.OutputFilePath, FILENAME + ".new");
            File.WriteAllText(path: newfile, contents: json);
            File.Delete(path: backpath);
            if (File.Exists(path: newfile))
            {
                if (!File.Exists(path: origpath))
                {
                    File.Create(path: origpath);
                }

                //    File.Replace(sourceFileName: newfile, destinationFileName: oldpath,destinationBackupFileName: backpath);
            }
        }
    }





    internal void SaveCacheIndex()
    {
        var path     = Path.Combine(path1: _options.OutputFilePath, FILENAME + ".new");
        var backpath = Path.Combine(path1: _options.OutputFilePath, FILENAME + ".bak");
        var oldpath  = Path.Combine(path1: _options.OutputFilePath, path2: FILENAME);


        SafeSerializeAndWrite(path: path, oldpath: oldpath, backpath: backpath);

        File.Delete(path: backpath);
    }





    private string TryGetFileNameFromCache(string uniqueUrlKey)
    {
        if (_cache.TryGetValue(key: uniqueUrlKey, out string cacheFileName))
        {
            Console.WriteLine($"cacne filename {cacheFileName}");
            return cacheFileName;
        }

        return null;
    }
}