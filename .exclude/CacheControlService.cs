#region

// ReSharper disable All
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

using KC.Apps.Logging;

using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;


#endregion

namespace KC.Apps.SpyderLib.Services ;

    /// <summary>
    ///     SpyderWeb Internal Web Content Module
    /// </summary>
    public class CacheControlService : CacheControlServiceBase, ICacheControlSvc
    {
        #region Volotiles

        private readonly CacheSignal<PageContent> _cacheSignal;

// Should not need initializer NotNull attribute used on initial method
        private CancellationTokenSource _cancellationTokenSource = null!;

        #endregion

        #region Setup/Teardown

        public CacheControlService(
            ILogger<CacheControlService> logger,
            IHostApplicationLifetime appLifetime,
            IOptions<SpyderOptions> options,
            CacheSignal<PageContent> cacheSignal,
            HttpClient httpClient) : base(logger, httpClient, options.Value)
            {
                _cache = LoadCacheIndex();
                _cacheSignal = cacheSignal;
                appLifetime.ApplicationStarted.Register(OnStarted);
                appLifetime.ApplicationStopping.Register(OnStopping);
                appLifetime.ApplicationStopped.Register(OnStopped);
            }

        #endregion

        //######################################################################################################3
        //******************************************************************************************************


        public async Task<PageContent> GetAndSetContentFromCacheAsync(string address)
            {
                PageContent result = new(address);
                try
                {
                    await _cacheSignal.WaitAsync();
                    return await GetAndSetContentFromCacheCoreAsync(address);
                }
                finally
                {
                    _cacheSignal.Release();
                }
            }





        /// <summary>
        ///     Internal method for cache operations. Will first attemtp to get the cache filename
        ///     from the in memory cache then return the contents of the file to the caller. If
        ///     a cache entry does not exist the source will be loaded from the web and saved
        ///     to the location set in options.
        /// </summary>
        /// <param name="address">Internet or Intranet address</param>
        /// <returns>String containing the page source for the address given</returns>
        public async Task<PageContent> GetAndSetContentFromCacheCoreAsync(string address)
            {
                PageContent resultObj = new(address);
                // due to the flow of this method we'll set fromcache to true initially;
                resultObj.FromCache = true;
                if (_cache is null) throw new ArgumentNullException(nameof(_cache));
                if (!_cache.TryGetValue(address, out string fname))
                {
                    // If there is an entry in cache this anon method will not be called
                    // otherwise lets get the content from the web and save to cache.
                    resultObj.FromCache = false;
                    resultObj.Content = await GetHttpContentFromWebAsync(address).ConfigureAwait(false);


                    //Get a unique filename to save the entry to disk.
                    // onlly the filename is in memory to reduce app memory.
                    var filename = GenerateUniqueCacheFilename();
                    resultObj.CacheFileName = filename;
                    if (!SafeFileWrite(Path.Combine(_options.CacheLocation, filename), resultObj.Content))
                    {
                        //Saving file to cache failed return null
                    }
                    _cache.TryAdd(address, filename);
                    return resultObj;
                }
                resultObj.CacheFileName = fname;
                resultObj.Content = File.ReadAllText(Path.Combine(_options.CacheLocation, fname));
                return resultObj;
            }





        /// <summary>
        /// Using HttpClient retrieve the contents of the page
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<string> GetHttpContentFromWebAsync(string validatedaddress)
            {
                int delay = 1000;
                int retries = 3;
                
                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        using var resp = await _httpClient.GetAsync(
                            requestUri: validatedaddress,
                            completionOption: HttpCompletionOption.ResponseContentRead);
                        if (resp.IsSuccessStatusCode)
                        {
                            return await resp.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            HandleHttpErrorResponse(resp);
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        _logger.LogHttpException( e.Message);
                        if (i == retries - 1) throw; // Re-throw the exception on the last retry

                        // Exponential delay
                        await Task.Delay(delay * (i + 1));
                    }
                }
                return string.Empty;
            }





        private async void HandleHttpErrorResponse(HttpResponseMessage resp)
            {
                switch (resp.StatusCode)
                {
                    case HttpStatusCode.Found:
                        var forwardaddress = resp.Headers.Location?.ToString();
                        if (!string.IsNullOrEmpty(forwardaddress))
                        {
                            var str = await GetHttpContentFromWebAsync(forwardaddress);
                        }
                        break;
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        _logger.PageCacheException(message: "Unauthorized web response. Moving on.");
                        break;
                    default:
                        // You can handle other Http status codes here.
                        break;
                }
            }





        public void VerifyIndexFileContents()
            {
                if (string.IsNullOrWhiteSpace(_options.CacheLocation))
                {
                    throw new ArgumentException("CacheLocation is null or empty");
                }

                // Load files from index location
                var files = GetExistingCacheFiles();
                var fileNames = files.Select(Path.GetFileName).ToList();
                _cache = LoadCacheIndex();

                // List files in cache without entries in index. (Possible incorrect cache location? Alert user.)
                //TODO: check for default random file generation pattern. don't want to dump cache files in important directories
                var orphanfiles = fileNames.Except(_cache.Select(c => c.Value));
                if (orphanfiles.Count() > 10)
                {
                    throw new Exception(
                        "Verify cache location. Multiple files found existing in cache location and are not in index. ");
                }

                // List index entries without coresponding files.
                var orphans = _cache.Where(pair => !fileNames.Contains(pair.Value)).ToList();
                foreach (var orphan in orphans)
                {
                    _cache.TryRemove(orphan);
                }
                SaveCacheIndex();
            }





        public void VerifySettings(SpyderOptions options)
            {
                if (options.UseLocalCache && !string.IsNullOrEmpty(options.CacheLocation))
                {
                    Directory.CreateDirectory(options.CacheLocation);
                    Directory.CreateDirectory(options.OutputFilePath);
                }
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
                _cache = LoadCacheIndex();
                // VerifyCache();
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





        public record struct PageContent
        {
            public PageContent(string url) : this()
                {
                    Url = url;
                    CacheFileName = string.Empty;
                    Content = string.Empty;
                    FromCache = false;
                }





            public string Url { get; init; }
            public string? CacheFileName { get; set; }
            public string? Content { get; set; }
            public bool FromCache { get; set; }
        }
    }