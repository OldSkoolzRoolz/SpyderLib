using System.Collections.Concurrent;
using System.Text;

using CommunityToolkit.Diagnostics;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Properties;

using Newtonsoft.Json;



namespace KC.Apps.SpyderLib.Modules;

public class FileOperations : IDisposable
{
    private bool _disposed;
    private readonly object _fileLock = new();
    private readonly SpyderOptions _options;
    private const int DELAY_ON_RETRY = 1000;
    private const string FILENAME = "Spyder_Cache_Index.json";
    private const int MAX_RETRIES = 3;






    public FileOperations()
        {
            _options = AppContext.GetData("options") as SpyderOptions;
            Guard.IsNotNull(_options);
            _ = Directory.CreateDirectory(_options.LogPath);
        }






    #region Public Methods

    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }






    public ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            var path = Path.Combine(_options.LogPath, FILENAME);


            if (!File.Exists(path))
                {
                    return new();
                }

            var json = File.ReadAllText(path);
            var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);


            return dict ?? new ConcurrentDictionary<string, string>();
        }






    /// <summary>
    ///     Asynchronously writes a specified contents to a specific file.
    ///     <param name="contents">
    ///         The contents to be written to the file.
    ///     </param>
    ///     <returns>
    ///         A task that represents the asynchronous operation.
    ///         The task result contains a boolean value indicating if the content was successfully written to the disk.
    ///         Returns <c>true</c> if content was successfully written to the disk, <c>false</c> otherwise.
    ///     </returns>
    ///     <exception cref="IOException">
    ///         Thrown when an error occurs during writing the contents to the file.
    ///     </exception>
    /// </summary>
    /// <returns></returns>
    public static Task SafeFileWriteAsync(
        string path,
        string contents)
        {
            try
                {
                    return File.WriteAllTextAsync(path, contents, Encoding.UTF8);
                }
            catch (IOException)
                {
                    throw new SpyderException(
                        "A file IO error occured saving file. Ensure permissions are valid.");
                }
        }






    public void SaveCacheIndex(
        [NotNull] ConcurrentDictionary<string, string> concurrentDictionary)
        {
            Guard.IsNotNull(concurrentDictionary);

            if (concurrentDictionary.IsEmpty)
                {
                    return;
                }

            try
                {
                    var path = Path.Combine(_options.LogPath, FILENAME + ".new");
                    var oldPath = Path.Combine(_options.LogPath, FILENAME);


                    SafeSerializeAndWrite(path, oldPath, concurrentDictionary);
                }
            catch
                {
                    throw new SpyderException("Failure to save cache index file");
                }
        }

    #endregion






    #region Private Methods

    protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                {
                    if (disposing)
                        {
                            // clean managed resources here
                        }

                    // clean unmanaged resources here
                    _disposed = true;
                }
        }






    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    ~FileOperations()
        {
            Dispose(false);
        }






    private void SafeSerializeAndWrite(
        string newFile,
        string originalFile,
        ConcurrentDictionary<string, string> indexCache)
        {
            ValidateParameters(newFile, originalFile, indexCache);

            lock (_fileLock)
                {
                    WriteToFile(newFile, originalFile, indexCache);
                }
        }






    private static void ValidateParameters(
        string newFile,
        string originalFile,
        ConcurrentDictionary<string, string> indexCache)
        {
            if (string.IsNullOrEmpty(newFile) || string.IsNullOrEmpty(originalFile) || indexCache == null)
                {
                    throw new ArgumentException("Invalid arguments.");
                }
        }






    private static void WriteToFile(
        string newFile,
        string originalFile,
        ConcurrentDictionary<string, string> indexCache)
        {
            for (var i = 0; i < MAX_RETRIES; ++i)
                {
                    try
                        {
                            var json = JsonConvert.SerializeObject(indexCache, Formatting.Indented);

                            if (File.Exists(newFile))
                                {
                                    File.Delete(newFile);
                                }

                            if (newFile != null)
                                {
                                    File.WriteAllText(newFile, json);

                                    if (!newFile.Equals(originalFile,
                                            StringComparison.OrdinalIgnoreCase) &&
                                        originalFile != null)
                                        {
                                            File.Copy(newFile, originalFile,
                                                true);
                                        }
                                }




                            break; // When done we can break loop
                        }
                    catch (IOException)
                        {
                            // You may check error code to filter some exceptions out 
                            if (i < MAX_RETRIES - 1) // i is zero-indexed, so we subtract one
                                {
                                    Thread.Sleep(DELAY_ON_RETRY); // Wait some time before retrying
                                }
                            else
                                {
                                    throw; // Re-throw the last exception
                                }
                        }
                }
        }

    #endregion
}