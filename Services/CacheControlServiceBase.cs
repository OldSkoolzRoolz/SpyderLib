using System.Collections.Concurrent;

using KC.Apps.Properties;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Newtonsoft.Json;


namespace KC.Apps.SpyderLib.Services;



public abstract class CacheControlServiceBase : BackgroundService
{
    private const string FILENAME = "Spyder_Cache_Index.json";
    protected static HttpClient _httpClient;
    private readonly object? _fileLock = new();
    protected readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1);
    protected IMemoryCache _cache;
    protected ILogger _logger;
    protected SpyderOptions _options;





    public CacheControlServiceBase(ILogger logger, HttpClient httpClient, SpyderOptions options)
        {
            _logger = logger ?? NullLogger.Instance;
            _httpClient = httpClient;
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }





    internal string? GetCacheContentFromDisk(string filename)
        {
            var path = Path.Combine(_options.CacheLocation, filename);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return null;
        }





    /// <summary>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    internal bool AddContentToCache(string address, string content)
        {
            var filename = GenerateUniqueCacheFilename();
            if (string.IsNullOrEmpty(filename))
            {
                _logger.LogCritical("Getting Unique filename failed");
            }

            var path = Path.Combine(_options.CacheLocation, filename);
            if (SafeFileWrite(path, content))
            {
                var cacheadded = AddToCache(address, filename);
                SaveCacheIndex();
                return cacheadded;
            }

            _logger.LogError("Failed to save cache file or db entry");
            return false;
        }





    protected bool AddToCache(string uniqueUrlKey, string cacheFileName)
        {
            MemoryCacheOptions coptions = new()
            {
                TrackStatistics = true, ExpirationScanFrequency = TimeSpan.FromMinutes(60)
            };

            MemoryCacheEntryOptions options = new()
            {
                SlidingExpiration = TimeSpan.FromDays(1)
            };

            _ = options.RegisterPostEvictionCallback(OnPostEviction);
            _cache.Set(uniqueUrlKey, cacheFileName, options);
            return true;
        }





    internal void DeleteFileInCache(string file)
        {
            var filePath = Path.Combine(_options.CacheLocation, file);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }





    internal string GenerateUniqueCacheFilename()
        {
            string filename;
            do
            {
                filename = Path.GetRandomFileName();
            } while (File.Exists(Path.Combine(_options.CacheLocation, filename)));

            return filename;
        }





    private IEnumerable<string?> GetExistingCacheFiles()
        {
            return Directory
                .EnumerateFiles(_options.CacheLocation)
                .Select(Path.GetFileName);
        }





    /// <summary>
    ///     Attempts to load an existing cache index file into memory
    /// </summary>
    /// <returns>Cache dictionary <string, string></returns>
    protected ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            var path = Path.Combine(_options.OutputFilePath, FILENAME);
            if (!File.Exists(path))
            {
                return new ConcurrentDictionary<string, string>();
            }

            try
            {
                lock (_fileLock!)
                {
                    var json = File.ReadAllText(path);
                    var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);
                    return dict ?? new ConcurrentDictionary<string, string>();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured when loading cached index");
                throw;
            }
        }





    /// <summary>
    /// </summary>
    /// <param name="path"></param>
    /// <param name="contents"></param>
    /// <returns></returns>
    internal bool SafeFileWrite(string path, string contents)
        {
            try
            {
                lock (_fileLock!)
                {
                    File.WriteAllText(path, contents);
                    if (!File.Exists(path))
                    {
                        Console.WriteLine("failed to save file");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured when writing file");
                return false;
            }
        }





    /// <summary>
    ///     Method serializes the cache index and saves files to disk after succesful save
    ///     old file is replaced with newly saved file. Helps to ensure the index is not corrupted during save.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    /// <param name="newfile"></param>
    /// <param name="originalfile"></param>
    /// <param name="backupfile"></param>
    internal void SafeSerializeAndWrite(string newfile, string originalfile, string backupfile)
        {
            lock (_fileLock!)
            {
                var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(newfile, json);
                if (File.Exists(newfile) && File.Exists(originalfile))
                {
                    File.Replace(newfile, originalfile, backupfile);
                    return;
                }

                File.Copy(newfile, originalfile);
            }
        }





    internal void SaveCacheIndex()
        {
            var path = Path.Combine(_options.OutputFilePath, FILENAME + ".new");
            var backpath = Path.Combine(_options.OutputFilePath, FILENAME + ".bak");
            var oldpath = Path.Combine(_options.OutputFilePath, FILENAME);
            SafeSerializeAndWrite(path, oldpath, backpath);
            File.Delete(backpath);
        }





    private void OnPostEviction(object key, object? value, EvictionReason reason, object? state)
        {
            Console.WriteLine($"value = {value} reason is {reason}");
        }





    protected string? TryGetFileNameFromCache(string uniqueUrlKey)
        {
            if (_cache.TryGetValue(uniqueUrlKey, out string? cacheFileName))
            {
                Console.WriteLine($"cacne filename {cacheFileName}");
                return cacheFileName;
            }

            return null;
        }
}