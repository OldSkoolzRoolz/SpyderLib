#region

using System.Collections.Concurrent;

using KC.Apps.Modules;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

#endregion




namespace KC.Apps.SpyderLib.Services;




public class IndexCacheService : BackgroundService
    {
        #region Instance variables

        private readonly CacheSignal<PageContent> _cacheSignal;
        private readonly IMyHttpClient _client;
        private readonly object _fileLock = new();
        private readonly ILogger _logger;

        private static SpyderOptions _options;
        private const string FILENAME = "Spyder_Cache_Index.json";

        #endregion





        public IndexCacheService(
            HttpClient factory,
            CacheSignal<PageContent> signal,
            IOptions<SpyderOptions> options)
            {
                _options = options.Value;
                _logger = SpyderControlService.LoggerFactory.CreateLogger<IndexCacheService>();
                _client = new MyHttpClient(factory, SpyderControlService.LoggerFactory.CreateLogger<MyHttpClient>());
                _cacheSignal = signal;
                this.IndexCache = new ConcurrentDictionary<string, string>();
                InitCache();
            }





        #region Prop

        public static int CacheHits { get; private set; }


        public static int CacheMisses { get; private set; }


        public ConcurrentDictionary<string, string> IndexCache { get; private set; }

        #endregion




        #region Methods

        public async Task<PageContent> GetAndSetContentFromCacheAsync(string address)
            {
                try
                    {
                        await _cacheSignal.WaitAsync().ConfigureAwait(false);
                        return await GetAndSetContentFromCacheCoreAsync(address).ConfigureAwait(false);
                    }
                finally
                    {
                        _cacheSignal.Release();
                    }
            }





        public void InitCache()
            {
                var cacheentries = LoadCacheIndex();
                this.IndexCache = cacheentries ?? new ConcurrentDictionary<string, string>();
                _logger.LogTrace("cache Index loaded {0} entries", this.IndexCache.Count);
            }





        public void SaveCacheIndex()
            {
                try
                    {
                        _cacheSignal.WaitAsync();
                        var path = Path.Combine(_options.LogPath, FILENAME + ".new");
                        var backpath = Path.Combine(_options.LogPath, FILENAME + ".bak");
                        var oldpath = Path.Combine(_options.LogPath, FILENAME);
                        SafeSerializeAndWrite(path, oldpath, backpath);
                    }
                catch
                    {
                        _logger.LogCritical("Failure saving Index file check logs and restart crawler.");
                        throw new SpyderException("Failure to save cache index file");
                    }
                finally
                    {
                        _cacheSignal.Release();
                    }
            }

        #endregion




        #region Methods

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                InitCache();
                while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(30000).ConfigureAwait(false);
                    }
            }





        /// <summary>
        ///     Attempts to load an existing cache index file into memory
        /// </summary>
        /// <returns>Cache dictionary <string, string></returns>
        protected ConcurrentDictionary<string, string> LoadCacheIndex()
            {
                var path = Path.Combine(_options.LogPath, FILENAME);
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

        #endregion




        #region Methods

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





        internal void SafeSerializeAndWrite(string newfile, string originalfile, string backupfile)
            {
                var allObjects = this.IndexCache.ToDictionary(
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

        #endregion




        #region Methods

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
                _logger.LogTrace("Entering method GetandSetContentFromCacheCore");
                _logger.LogTrace("Loading page source for page {0}", address);
                PageContent resultObj = new(address);

                //  set from cache to true initially;
                resultObj.FromCache = true;
                if (!this.IndexCache.TryGetValue(address, out var fname))
                    {
                        _logger.LogTrace("Address was not found in cache loading page");

                        // If there is an entry in cache this anon method will not be called
                        // otherwise lets get the content from the web and save to cache.
                        CacheMisses++;
                        resultObj.FromCache = false;
                        resultObj.Content = await _client.GetHttpContentFromWebAsync(address).ConfigureAwait(false);
                        _logger.LogTrace(
                            "Page content retrieved from web with length of:{0}.", resultObj.Content.Length);

                        //Get a unique filename to save the entry to disk.
                        // onlly the filename is in memory to reduce app memory.
                        var filename = GenerateUniqueCacheFilename();
                        resultObj.CacheFileName = filename;
                        _logger.LogTrace("Filename of new cache entry {0}", filename);
                        if (!SafeFileWrite(Path.Combine(_options.CacheLocation, filename), resultObj.Content))
                            {
                                //Saving file to cache failed return null
                                _logger.LogError(
                                    "Failed to save entry to disk. Continuing...If this error continues check your settings and try again.");
                            }

                        // as an atttempt to keep cache clean if content length small don't save
                        if (resultObj.Content.Length > 100)
                            {
                                var filesaved = this.IndexCache.TryAdd(address, filename);
                                _logger.LogTrace("Status of TryAdd entry to in-memory index: {0}", filesaved);
                            }

                        return resultObj;
                    }

                //Entry was found in cache
                resultObj.CacheFileName = fname;
                _logger.LogTrace("Reading cache entry from disk");
                resultObj.Content = File.ReadAllText(Path.Combine(_options.CacheLocation, fname));
                _logger.LogTrace("Returning page content obj");
                CacheHits++;
                return resultObj;
            }

        #endregion
    }




public record struct PageContent
    {
        public PageContent(string url) : this()
            {
                this.Url = url;
                this.CacheFileName = string.Empty;
                this.Content = string.Empty;
                this.FromCache = false;
            }





        #region Prop

        public string? CacheFileName { get; set; }
        public string? Content { get; set; }
        public bool FromCache { get; set; }


        public string Url { get; init; }

        #endregion
    }




//namespace