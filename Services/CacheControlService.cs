#region

// ReSharper disable All
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;

using KC.Apps.Control;
using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.Spyderlib.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using PuppeteerSharp;

#endregion

namespace KC.Apps.SpyderLib.Services;



public interface ICacheControlService
{
    /// <summary>
    ///     Method returns page content from cache if available
    ///     or gets content from the web and caches the filename of the save file.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    Task<string> GetAndSetContentFromCacheAsync(string address);
}




/// <summary>
///     SpyderWeb Internal Web Content Module
/// </summary>
public class CacheControlService : CacheControlServiceBase, ICacheControlService
{
    private readonly CacheSignal<PageContent> _cacheSignal;

// Should not need initializer NotNull attribute used on initial method
    private CancellationTokenSource _cancellationTokenSource = null!;
    private bool _isCacheInitialized;





    public CacheControlService(
        ILogger<CacheControlService> logger,
        IHostApplicationLifetime appLifetime,
        IOptions<SpyderOptions> options,
        CacheSignal<PageContent> cacheSignal,
        HttpClient httpClient,
        IMemoryCache cache) : base(logger, httpClient, options.Value)
        {
            _cacheSignal = cacheSignal;
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }





    //######################################################################################################3
    //******************************************************************************************************





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
            string pageFilename = (await _cache.GetOrCreateAsync(
                address, async (cacheEntry) =>
                {
                    // If there is an entry in cache this anon method will not be called
                    // otherwise lets get the content from the web and save to cache.
                    var pageContent = await GetHttpContentFromWebAsync(address).ConfigureAwait(false);
                    //If there is no data to return, dispose of cache entry is called
                    // to prevent a null object saved to cache
                    if (pageContent is null)
                    {
                        cacheEntry.Dispose();
                        return null;
                    }

                    //Get a unique filename to save the entry to disk.
                    // onlly the filename is in memory to reduce app memory.
                    var filename = GenerateUniqueCacheFilename();
                    //Save page to cache location on disk
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
    /// <param name="address"></param>
    /// <returns></returns>
    public async Task<string> GetHttpContentFromWebAsync(string address)
        {
            var content = string.Empty;
            try
            {
                var resp = await _httpClient.GetAsync(
                    requestUri: address,
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
                        _logger.PageCacheException(message: "Unauthorized web response. Moving on.");
                        break;
                    }
                    case HttpStatusCode.Forbidden:
                    {
                        //we get the boot again. Rinse and repeat....
                        _logger.PageCacheException(message: "Unauthorized web response. Moving on.");
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
            catch (Exception e)
            {
                // Errors here should be avoided at all costs.
                _logger.UnexpectedResultsException(message: e.Message);
            }

            return content;
        }





    private void OnStopped()
        {
            _logger.DebugTestingMessage("Cache Control Module is unloaded..");
        }





    private void OnStopping()
        {
            SaveCacheIndex();
            _logger.DebugTestingMessage("Cache Index saved...");
            _cancellationTokenSource?.Cancel();
        }





    private void OnStarted()
        {
            _logger.DebugTestingMessage("Cache Control Started..");
            VerifySettings(_options);
            VerifyCache();
            PopulateCacheFromFile();
        }





    private void VerifyCache()
        {
            var files = Directory.EnumerateFiles(_options.CacheLocation);
            var cache = LoadCacheIndex();
            var orphans = cache.Select(s => s.Value)
                .Except(files);

            var orphanfiles = files.Select(file => file)
                .Except(cache.Select(c => c.Key));

            foreach (var orph in orphans)
            {
                Console.WriteLine(orph);
            }
        }





    private void VerifySettings(SpyderOptions options)
        {
            if (options.UseLocalCache && !string.IsNullOrEmpty(options.CacheLocation))
            {
                Directory.CreateDirectory(options.CacheLocation);
                Directory.CreateDirectory(options.OutputFilePath);
            }
        }





    private void PopulateCacheFromFile()
        {
            _logger.LogInformation(message: "Loading cache.");
            try
            {
                var diskcache = LoadCacheIndex();
                if (diskcache.IsEmpty || diskcache is null)
                {
                    _logger.DebugTestingMessage("Load cache returned null..");
                    return;
                }

                foreach (var item in diskcache)
                {
                    var path = Path.Combine(_options.CacheLocation, item.Value);
                    if (File.Exists(path))
                    {
                        AddToCache(uniqueUrlKey: item.Key, cacheFileName: item.Value);
                    }
                }

                //  var stats = _cache.GetCurrentStatistics();
                _logger.DebugTestingMessage("Cache loaded from disk record count={count}");
            }
            finally
            {
                _isCacheInitialized = true;
            }
        }





    [MemberNotNull(nameof(_cancellationTokenSource))]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            await Task.Yield();
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(3000);
            }
        }





    public record struct PageContent(
        string url,
        string fileName,
        string content);
}