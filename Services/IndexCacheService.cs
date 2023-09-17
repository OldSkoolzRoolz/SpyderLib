using System.Collections.Concurrent;
using System.Net;
using System.Runtime.Caching;

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.Spyderlib.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using SpyderLib;


namespace KC.Apps.SpyderLib.Services;



public class IndexCacheService : BackgroundService
{
    private const string FILENAME = "Spyder_Cache_Index.json";
    private readonly ObjectCache _cacheObj;
    private readonly CacheSignal<CacheItem> _cacheSignal;
    private readonly object _fileLock = new();
    private readonly ILogger<IndexCacheService> _logger;
    private readonly SpyderOptions _options;
    protected HttpClient _httpClient;

//private CacheItem<Dictionary<string, string>() _item = new CacheItem()





    public IndexCacheService(
        ILogger<IndexCacheService> logger,
        IOptions<SpyderOptions> options,
        HttpClient httpClient,
        CacheSignal<CacheItem> signal)
        {
            _logger = logger ?? NullLogger<IndexCacheService>.Instance;
            _options = options.Value;
            _httpClient = httpClient;
            _cacheSignal = signal;
            _cacheObj = MemoryCache.Default;
        }





    internal void InitCache()
        {
            var cacheentries = LoadCacheIndex();
            foreach (var entry in cacheentries)
            {
                var policy = new CacheItemPolicy
                {
                    Priority = CacheItemPriority.Default, AbsoluteExpiration = DateTime.Now.AddDays(7)
                };

                _cacheObj.Add(entry.Key, entry.Value, policy);
            }
        }





    public async Task<string> GetAndSetContentFromCacheAsync(string address)
        {
            string content;
            try
            {
                await _cacheSignal.WaitAsync();
                content = await GetAndSetContentFromCacheCoreAsync(address);
            }
            finally
            {
                _cacheSignal.Release();
            }

            return content;
        }





    /// <summary>
    ///     Internal method for cache operations. Will first attemtp to get the cache filename
    ///     from the in memory cache then return the contents of the file to the caller. If
    ///     a cache entry does not exist the source will be loaded from the web and saved
    ///     to the location set in options.
    /// </summary>
    /// <param name="address">Internet or Intranet address</param>
    /// <returns>String containing the page source for the address given</returns>
    private async Task<string> GetAndSetContentFromCacheCoreAsync(string address)
        {
            //  string pageContent = string.Empty;
            var pageFilename = (await _cacheObj.GetOrCreateAsync(
                address, async cacheEntry =>
                {
                    // If there is an entry in cache this anon method will not be called
                    // otherwise lets get the content from the web and save to cache.
                    var pageContent = await GetHttpContentFromWebAsync(address).ConfigureAwait(false);

                    //If there is no data to return, dispose of cache entry is called
                    // to prevent a null object saved to cache
                    if (pageContent is null)
                    {
                        //  cacheEntry.Dispose();
                        return null;
                    }

                    //Get a unique filename to save the entry to disk.
                    // onlly the filename is in memory to reduce app memory.
                    var filename = GenerateUniqueCacheFilename();

                    //Save page to cache location on disk
                    // filename and url address is added to cache. Content is saved to disk
                    if (!SafeFileWrite(Path.Combine(_options.CacheLocation, filename), pageContent))
                    {
                        //Cache file save was successful return filename
                        //filename is returned to the cache method and saved in cache
                        return null;
                    }

                    cacheEntry.Value = filename;
                    // return the filename to the GetOrCreateMethod as the cacheentry
                    return pageContent;
                }).ConfigureAwait(false))!;

            return pageFilename;
            //
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





    internal string GenerateUniqueCacheFilename()
        {
            string filename;
            do
            {
                filename = Path.GetRandomFileName();
            } while (File.Exists(Path.Combine(_options.CacheLocation, filename)));

            return filename;
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
                var resp = await _httpClient.GetAsync(
                    address,
                    HttpCompletionOption.ResponseContentRead);

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
                        var forwardaddress = resp.Headers.Location?.ToString();
                        if (!string.IsNullOrEmpty(forwardaddress))
                        {
                            var str = await GetHttpContentFromWebAsync(forwardaddress);
                        }

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
                    case HttpStatusCode.Continue:
                        break;
                    case HttpStatusCode.SwitchingProtocols:
                        break;
                    case HttpStatusCode.Processing:
                        break;
                    case HttpStatusCode.EarlyHints:
                        break;
                    case HttpStatusCode.OK:
                        break;
                    case HttpStatusCode.Created:
                        break;
                    case HttpStatusCode.Accepted:
                        break;
                    case HttpStatusCode.NonAuthoritativeInformation:
                        break;
                    case HttpStatusCode.NoContent:
                        break;
                    case HttpStatusCode.ResetContent:
                        break;
                    case HttpStatusCode.PartialContent:
                        break;
                    case HttpStatusCode.MultiStatus:
                        break;
                    case HttpStatusCode.AlreadyReported:
                        break;
                    case HttpStatusCode.IMUsed:
                        break;
                    case HttpStatusCode.Ambiguous:
                        break;
                    case HttpStatusCode.Moved:
                        break;
                    case HttpStatusCode.RedirectMethod:
                        break;
                    case HttpStatusCode.NotModified:
                        break;
                    case HttpStatusCode.UseProxy:
                        break;
                    case HttpStatusCode.Unused:
                        break;
                    case HttpStatusCode.RedirectKeepVerb:
                        break;
                    case HttpStatusCode.PermanentRedirect:
                        break;
                    case HttpStatusCode.BadRequest:
                        break;
                    case HttpStatusCode.PaymentRequired:
                        break;
                    case HttpStatusCode.NotFound:
                        break;
                    case HttpStatusCode.MethodNotAllowed:
                        break;
                    case HttpStatusCode.NotAcceptable:
                        break;
                    case HttpStatusCode.ProxyAuthenticationRequired:
                        break;
                    case HttpStatusCode.RequestTimeout:
                        break;
                    case HttpStatusCode.Conflict:
                        break;
                    case HttpStatusCode.Gone:
                        break;
                    case HttpStatusCode.LengthRequired:
                        break;
                    case HttpStatusCode.PreconditionFailed:
                        break;
                    case HttpStatusCode.RequestEntityTooLarge:
                        break;
                    case HttpStatusCode.RequestUriTooLong:
                        break;
                    case HttpStatusCode.UnsupportedMediaType:
                        break;
                    case HttpStatusCode.RequestedRangeNotSatisfiable:
                        break;
                    case HttpStatusCode.ExpectationFailed:
                        break;
                    case HttpStatusCode.MisdirectedRequest:
                        break;
                    case HttpStatusCode.UnprocessableEntity:
                        break;
                    case HttpStatusCode.Locked:
                        break;
                    case HttpStatusCode.FailedDependency:
                        break;
                    case HttpStatusCode.UpgradeRequired:
                        break;
                    case HttpStatusCode.PreconditionRequired:
                        break;
                    case HttpStatusCode.TooManyRequests:
                        break;
                    case HttpStatusCode.RequestHeaderFieldsTooLarge:
                        break;
                    case HttpStatusCode.UnavailableForLegalReasons:
                        break;
                    case HttpStatusCode.InternalServerError:
                        break;
                    case HttpStatusCode.NotImplemented:
                        break;
                    case HttpStatusCode.BadGateway:
                        break;
                    case HttpStatusCode.ServiceUnavailable:
                        break;
                    case HttpStatusCode.GatewayTimeout:
                        break;
                    case HttpStatusCode.HttpVersionNotSupported:
                        break;
                    case HttpStatusCode.VariantAlsoNegotiates:
                        break;
                    case HttpStatusCode.InsufficientStorage:
                        break;
                    case HttpStatusCode.LoopDetected:
                        break;
                    case HttpStatusCode.NotExtended:
                        break;
                    case HttpStatusCode.NetworkAuthenticationRequired:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (TaskCanceledException)
            {
                //continue on....
            }
            catch (Exception e)
            {
                //swallow err
                // Errors here should be avoided at all costs.
                _logger.UnexpectedResultsException(e.Message);
            }

            return content;
        }





    public void SaveCacheIndex()
        {
            try
            {
                _cacheSignal.WaitAsync();
                var path = Path.Combine(_options.OutputFilePath, FILENAME + ".new");
                var backpath = Path.Combine(_options.OutputFilePath, FILENAME + ".bak");
                var oldpath = Path.Combine(_options.OutputFilePath, FILENAME);
                SafeSerializeAndWrite(path, oldpath, backpath);
            }
            finally
            {
                _cacheSignal.Release();
            }
        }





    internal void SafeSerializeAndWrite(string newfile, string originalfile, string backupfile)
        {
            var allObjects = _cacheObj.ToDictionary(
                cachedObject => cachedObject.Key,
                cachedObject => cachedObject.Value
            );

            var json = JsonConvert.SerializeObject(allObjects, Formatting.Indented);
            File.WriteAllText(newfile, json);
            if (File.Exists(newfile) && File.Exists(originalfile))
            {
                File.Replace(newfile, originalfile, backupfile);
                return;
            }

            File.Copy(newfile, originalfile);
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





    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            InitCache();
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(30000);
            }
        }
}
//namespace