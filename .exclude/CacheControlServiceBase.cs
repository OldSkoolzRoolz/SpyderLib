#region

using System.Collections.Concurrent;
using KC.Apps.SpyderLib.Properties;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Newtonsoft.Json;


#endregion

namespace KC.Apps.SpyderLib.Services ;

    public abstract class CacheControlServiceBase : BackgroundService
    {
        #region Volotiles

        protected ConcurrentDictionary<string, string> _cache = new();
        private readonly object? _fileLock = new();
        protected static HttpClient _httpClient;
        protected ILogger _logger;
        protected SpyderOptions _options;
        protected readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1);
        private const string FILENAME = "Spyder_Cache_Index.json";

        #endregion

        #region Setup/Teardown

        public CacheControlServiceBase(ILogger logger, HttpClient httpClient, SpyderOptions options)
            {
                _logger = logger ?? NullLogger.Instance;
                _httpClient = httpClient;
                _options = options;
            }

        #endregion

        public string GenerateUniqueCacheFilename()
            {
                if (!Path.Exists(_options.CacheLocation)) throw new ArgumentNullException("Cache LOcation is not set");
                
                
                string filename;
                do
                {
                    filename = Path.GetRandomFileName();
                } while (File.Exists(Path.Combine(_options.CacheLocation, filename)));
                return filename;
            }





        public IEnumerable<string?> GetExistingCacheFiles()
            {
                return Directory
                    .EnumerateFiles(_options.CacheLocation)
                    .Select(Path.GetFileName);
            }





        /// <summary>
        ///     Attempts to load an existing cache index file into memory
        /// </summary>
        /// <returns>Cache dictionary <string, string></returns>
        public ConcurrentDictionary<string, string> LoadCacheIndex()
            {
                var path = Path.Combine(AppContext.BaseDirectory, FILENAME);
                if (!File.Exists(path))
                {
                    return new ConcurrentDictionary<string, string>();
                }
                try
                {
                    lock (_fileLock!)
                    {
                        var json = File.ReadAllText(path);
                        if (!string.IsNullOrWhiteSpace(json) )
                        {
                            var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);
                            return dict ?? new ConcurrentDictionary<string, string>();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception occured when loading cached index");
                    throw;
                }
                return new();
            }





        /// <summary>
        ///     Safe method used for saving a file to disk, such as the cache files/
        /// </summary>
        /// <param name="path">path used as local cache location. Filename is a random generated name.</param>
        /// <param name="contents">Contents of the file is the page sources</param>
        /// <returns>a boolean indicating success or faiGetVlure.</returns>
        public bool SafeFileWrite(string path, string contents)
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
        public void SafeSerializeAndWrite(string newfile, string originalfile, string backupfile)
            {
                lock (_fileLock!)
                {
                    var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                    File.AppendAllText(newfile, json);
                    if (File.Exists(newfile) && File.Exists(originalfile))
                    {
                        File.Replace(newfile, originalfile, backupfile);
                        return;
                    }
                    File.Copy(newfile, originalfile);
                }
            }





        public void SaveCacheIndex()
            {
                var basepath = Path.Combine(AppContext.BaseDirectory, FILENAME);
                var path = basepath + ".new";
                var backpath = basepath + ".bak";
                var oldpath = basepath;
                SafeSerializeAndWrite(path, oldpath, backpath);
                File.Delete(backpath);
            }
    }