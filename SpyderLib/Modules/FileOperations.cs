using System.Collections.Concurrent;
using KC.Apps.Properties;
using Newtonsoft.Json;

namespace KC.Apps.SpyderLib.Modules;

public class FileOperations : IDisposable
{
    private readonly SpyderOptions _options;





    public FileOperations(SpyderOptions options)
        {
            _options = options;
        }





    public void Dispose()
        {
            // TODO release managed resources here
        }





    private const string FILENAME = "Spyder_Cache_Index.json";





    /// <summary>
    /// </summary>
    /// <param name="path"></param>
    /// <param name="contents"></param>
    /// <returns></returns>
    public async Task<bool> SafeFileWriteAsync(string path, string contents)
        {
            try
                {

                    await File.WriteAllTextAsync(path, contents).ConfigureAwait(false);
                    if (!File.Exists(path))
                        {
                            Console.WriteLine("failed to save file");
                            return false;
                        }

                    return true;
                }
            catch (Exception)
                {
                    return false;
                }
        }





    public void SaveCacheIndex(SpyderOptions options, ConcurrentDictionary<string, string> concurrentDictionary)
        {
            try
                {
                    var path = Path.Combine(options.LogPath, FILENAME + ".new");
                    var backpath = Path.Combine(options.LogPath, FILENAME + ".bak");
                    var oldpath = Path.Combine(options.LogPath, FILENAME);
                    SafeSerializeAndWrite(path, oldpath, backpath, concurrentDictionary);
                }
            catch
                {
                    throw new SpyderException("Failure to save cache index file");
                }

        }





    public async Task<ConcurrentDictionary<string, string>> LoadCacheIndexAsync()
        {
            var path = Path.Combine(_options.LogPath, FILENAME);

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);
            return dict ?? new ConcurrentDictionary<string, string>();
        }





    public void SafeSerializeAndWrite(string newfile, string originalfile, string backupfile,
                                      ConcurrentDictionary<string, string> indexCache)
        {

            var allObjects = indexCache.ToDictionary(
                                                     cachedObject => cachedObject.Key,
                                                     cachedObject => cachedObject.Value
                                                    );

            var json = JsonConvert.SerializeObject(allObjects, Formatting.Indented);
            File.WriteAllText(newfile, json);
            if (File.Exists(newfile) && File.Exists(originalfile))
                {
                    File.Replace(newfile, originalfile, backupfile);
                }
            else if (File.Exists(originalfile))
                {
                    File.Copy(newfile, originalfile);
                }
        }
}