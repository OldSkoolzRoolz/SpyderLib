#region

using System.Collections.Concurrent;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : ServiceBase
{
    private readonly MyHttpClient _client;
    private readonly ILogger _logger;





    public CacheIndexService(
        ILoggerFactory loggerFactory, IHostApplicationLifetime applicationLifetime)
        {
            _logger = loggerFactory.CreateLogger<CacheIndexService>();
            _client = new MyHttpClient(_logger);
            applicationLifetime.ApplicationStarted.Register(Initialize);
        }





    private void Initialize()
        {

            this.IndexCache = LoadCacheIndex();

            lock (this.IndexCache)
                {
                    VerifyCacheIndex();
                }
        }





    private ConcurrentDictionary<string, string> IndexCache { get; set; }





    private ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            var fileOperations = new FileOperations(this.Options);
            try
                {
                    // load the cache asynchronously to avoid blocking the constructor
                    return fileOperations.LoadCacheIndexAsync().Result;
                }
            catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
        }





    public static int CacheHits { get; private set; }


    public static int CacheMisses { get; private set; }





    public async Task<PageContent> GetAndSetContentFromCacheAsync(string address)
        {
            return await GetAndSetContentFromCacheCoreAsync(address).ConfigureAwait(false);
        }





    public void SaveCacheIndexPublicWrapper()
        {
            var fileOperations = new FileOperations(this.Options);
            fileOperations.SaveCacheIndex(this.Options, this.IndexCache);
        }





    public async Task<PageContent> SetContentCachePublicWrapperAsync(string content, string address)
        {
            return await SetContentCacheAsync(content, address).ConfigureAwait(false);
        }





    /// <summary>
    ///     Verifies the cache index by performing cleaning of non-existing files and entries.
    /// </summary>
    private void VerifyCacheIndex()
        {
            // Create copies so we don't iterate live data.
            var cacheEntriesSnapshot = new Dictionary<string, string>(this.IndexCache);
            var directoryFilesSnapshot = Directory.GetFiles(this.Options.CacheLocation);

            var deletedFilesCount = 0;
            var deletedEntriesCount = 0;

            // Iterate through a copy of the cache index and deletes entries if files don't exist.
            foreach (var item in cacheEntriesSnapshot)
                {
                    var entryFilePath = Path.Combine(this.Options.CacheLocation, item.Value);
                    if (File.Exists(entryFilePath))
                        {
                            continue;
                        }

                    if (this.IndexCache.Remove(item.Key, out _))
                        {
                            deletedEntriesCount++;
                        }
                }

            // Compare existing files in directory with cache entries.
            var currentCacheFileNames =
                new HashSet<string>(this.IndexCache.Values.Select(filename =>
                                                                      Path.Combine(this.Options.CacheLocation,
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
                    catch (Exception e)
                        {
                            _logger.LogWarning(e,
                                               $"Failed to delete file: {file}. It can be either used by another process or doesn't exist anymore.");
                        }
                }

            SaveCacheIndexPublicWrapper();
            _logger.LogInformation($"Cache verification complete. {deletedFilesCount} files and {deletedEntriesCount} entries deleted.");
        }





    private static string GenerateUniqueCacheFilename(string cacheLocale)
        {
            string filename;
            do
                {
                    filename = Path.GetRandomFileName();
                } while (File.Exists(Path.Combine(cacheLocale, filename)));

            return filename;
        }





    /// <summary>
    ///     Internal method for cache operations. Will first attemtp to get the cache filename
    ///     from the in memory cache then return the contents of the file to the caller. If
    ///     a cache entry does not exist the source will be loaded from the web and saved
    ///     to the location set in options.
    /// </summary>
    /// <param name="address">Internet or Intranet address</param>
    /// <returns>String containing the page source for the address given</returns>
    private async Task<PageContent> GetAndSetContentFromCacheCoreAsync(string address)
        {
            PageContent resultObj = new(address);

            if (!this.IndexCache.TryGetValue(address, out var filename))
                {
                    // Load content from web
                    _logger.LogTrace("WEB: Loading page {0}", address);
                    var content = await _client.GetContentFromWebWithRetryAsync(address).ConfigureAwait(false);

                    return await SetContentCacheAsync(content, address).ConfigureAwait(false);
                }

            //Entry was found in cache
            _logger.LogTrace("CACHE: Loading page {0}", address);
            var cacheentry = Path.Combine(this.Options.CacheLocation, filename);

            if (File.Exists(cacheentry))
                {
                    resultObj.CacheFileName = filename;
                    _logger.LogTrace("Reading cache entry from disk");

                    resultObj.Content = await File.ReadAllTextAsync(Path.Combine(this.Options.CacheLocation, filename))
                                                  .ConfigureAwait(false);
                    CacheHits++;
                }
            else
                {
                    _logger.LogCritical("A cache index consistency check has been triggered. Checking cache consitency...");
                    VerifyCacheIndex();
                }

            return resultObj;
        }





    private async Task<PageContent> SetContentCacheAsync(string content, string address)
        {
            var fileOperations = new FileOperations(this.Options);
            PageContent resultObj = new(address);
            resultObj.FromCache = false;
            resultObj.Content = content;
            try
                {
                    _logger.LogTrace("Address was not found in cache loading page");
                    CacheMisses++;
                    if (content.Length > 1200)
                        {
                            _logger.LogTrace("Page content retrieved from web with length of:{0}.",
                                             resultObj.Content.Length);

                            //Generate a unique filename to save the entry to disk.
                            var filename = GenerateUniqueCacheFilename(this.Options.CacheLocation);
                            resultObj.CacheFileName = filename;

                            var filesaved = await
                                fileOperations.SafeFileWriteAsync(Path.Combine(this.Options.CacheLocation, filename),
                                                                  resultObj.Content).ConfigureAwait(false);
                            if (filesaved)
                                {
                                    this.IndexCache.TryAdd(address, filename);
                                }
                        }
                }
            catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                }


            return resultObj;
        }
}

public class CacheFileHandlerService
{
    public async Task<string> ReadFileContentsAsync(string path, string fileName)
        {
            var fullPath = Path.Combine(path, fileName);
            return await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
        }
} //namespace