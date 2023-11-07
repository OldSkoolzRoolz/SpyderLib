#region

using System.Collections.Concurrent;

using HtmlAgilityPack;

using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : ICacheIndexService
{
    #region Other Fields

    private readonly ISpyderClient _client;
    private readonly ILogger _logger;
    private readonly SpyderOptions _options;
    private readonly IOutputControl _output;

    #endregion

    #region Properteez

    private ConcurrentDictionary<string, string> IndexCache { get; set; }

    #endregion

    #region Interface Members

    public async Task<PageContent> GetAndSetContentFromCacheAsync(
        string address)
        {
            return await GetAndSetContentFromCacheCoreAsync(address).ConfigureAwait(false);
        }





    public void SaveCacheIndex()
        {
            using var fileOperations = new FileOperations(_options);
            fileOperations.SaveCacheIndex(_options, this.IndexCache);
        }





    public async Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address)
        {
            return await SetContentCacheAsync(content, address).ConfigureAwait(false);
        }





    public async Task<IEnumerable<string>> GetLinksFromPageContentAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                {
                    throw new ArgumentException("Url can't be null or empty", nameof(url));
                }

            HtmlDocument doc;
            try
                {
                    doc = await ConvertToDocumentObjectAsync(url);
                }
            catch (Exception e)
                {
                    // Log the exception
                    throw new ApplicationException($"Failed to convert URL to document object: {url}", e);
                }

            IEnumerable<string> links;
            try
                {
                    links = HtmlParser.GetHrefLinksFromDocument(doc);
                }
            catch (Exception e)
                {
                    // Log the exception
                    throw new ApplicationException("Failed to extract links from document object", e);
                }


            return links;
        }

    #endregion

    #region Public Methods

    public CacheIndexService(
        ILogger<CacheIndexService> logger,
        IOptions<SpyderOptions>    options)
        {
            _logger = logger;
            _options = options.Value;
            _client = new SpyderClient(_logger);
            _output = OutputControl.Instance;
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
            IndexCache = LoadCacheIndex();
            Log.Information("Cache Index Service Loaded...");
        }





    public static int CacheHits { get; private set; }


    public static int CacheMisses { get; private set; }
    public static TaskCompletionSource<bool> StartupComplete { get; set; } = new();

    #endregion

    #region Private Methods

    private void OnStarted()
        {
            this.IndexCache = LoadCacheIndex();
            StartupComplete.TrySetResult(true);
        }





    private void OnStopping(object sender, EventArgs eventArgs)
        {
            Log.Trace("Cache service is shutting down...");

            try
                {
                    SaveCacheIndex();
                    Log.Information("Cache Index is saved");
                }
            catch (Exception)
                {
                    Log.Error("Error saving Cache Index");

                }
        }





    private async Task<HtmlDocument> ConvertToDocumentObjectAsync(string url)
        {

            var page = await GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);

            var doc = new HtmlDocument();
            doc.LoadHtml(page.Content);


            return doc;
        }





 










    private ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            var fileOperations = new FileOperations(_options);
            try
                {
                    // load the cache asynchronously to avoid blocking the constructor
                    return fileOperations.LoadCacheIndex();
                }
            catch (Exception)
                {
                    _logger.LogError("Failed to load cache index");


                    throw;
                }
        }





    /// <summary>
    ///     Verifies the cache index by performing cleaning of non-existing files and entries.
    /// </summary>
    private void VerifyCacheIndex()
        {
            // Create copies so we don't iterate live data.
            var cacheEntriesSnapshot = new Dictionary<string, string>(this.IndexCache);
            var directoryFilesSnapshot = Directory.GetFiles(_options.CacheLocation);

            var deletedFilesCount = 0;
            var deletedEntriesCount = 0;

            // Iterate through a copy of the cache index and deletes entries if files don't exist.
            foreach (var item in cacheEntriesSnapshot)
                {
                    var entryFilePath = Path.Combine(_options.CacheLocation, item.Value);
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
                    catch (Exception e)
                        {
                            _logger.LogWarning(e,
                                               $"Failed to delete file: {file}. It can be either used by another process or doesn't exist anymore.");
                        }
                }

            SaveCacheIndex();
            _logger.LogInformation($"Cache verification complete. {deletedFilesCount} files and {deletedEntriesCount} entries deleted.");
        }





    private static string GenerateUniqueCacheFilename(
        string cacheLocale)
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
    private async Task<PageContent> GetAndSetContentFromCacheCoreAsync(
        string address)
        {
            PageContent resultObj = new(address);
            // Check Cache if address exists if entry wss not found load it from web
            if (!this.IndexCache.TryGetValue(address, out var filename))
                {
                    // Entry was not found in cache so Load content from web
                    _logger.LogTrace("WEB: Loading page {0}", address);
                    
                    var content = await _client.GetContentFromWebWithRetryAsync(address).ConfigureAwait(false);


                    return await SetContentCacheAsync(content, address).ConfigureAwait(false);
                }

            
            
            
            //Entry was found in cache
            _logger.LogDebug("CACHE: Loading page {0}", address);
            var cacheentry = Path.Combine(_options.CacheLocation, filename);

            if (File.Exists(cacheentry))
                {
                    resultObj.CacheFileName = filename;
                    // _logger.LogTrace("Reading cache entry from disk");

                    resultObj.Content = await File.ReadAllTextAsync(Path.Combine(_options.CacheLocation, filename))
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




/// <summary>
/// Save page text to disk and add cache entry for address
/// </summary>
/// <param name="content"></param>
/// <param name="address"></param>
/// <returns></returns>
    private async Task<PageContent> SetContentCacheAsync(
        string content,
        string address)
        {
            var fileOperations = new FileOperations(_options);
            PageContent resultObj = new(address);
            resultObj.FromCache = false;
            resultObj.Content = content;
            try
                {
                    // Verify that key does not exist already in cache.
                    if (IndexCache.ContainsKey(address))
                        {
                            return resultObj;
                            
                        }
                    
                    //  _logger.LogTrace("Address was not found in cache loading page");
                    CacheMisses++;
                    if (content.Length > 1200)
                        {
                            _logger.LogTrace("Page content retrieved from web with length of:{0}.",
                                             resultObj.Content.Length);

                            //Generate a unique filename to save the entry to disk.
                            var filename = GenerateUniqueCacheFilename(_options.CacheLocation);
                            resultObj.CacheFileName = filename;

                            var filesaved = await
                                fileOperations.SafeFileWriteAsync(Path.Combine(_options.CacheLocation, filename),
                                                                  resultObj.Content).ConfigureAwait(false);
                           //verify successful save before adding entry to cache
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

    #endregion
}

public class CacheFileHandlerService
{
    #region Public Methods

    public async Task<string> ReadFileContentsAsync(
        string path,
        string fileName)
        {
            var fullPath = Path.Combine(path, fileName);


            return await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
        }

    #endregion
} //namespace