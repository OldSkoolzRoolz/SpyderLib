#region

using System.Collections.Concurrent;

using KC.Apps.Properties;
using KC.Apps.SpyderLib.Logging;

using Newtonsoft.Json;

#endregion


namespace KC.Apps.SpyderLib.Modules;

public class FileOperations : IDisposable
{
    #region Feeelldzz

    private const string FILENAME = "Spyder_Cache_Index.json";

    private static readonly int _maxRetries = 3;
    private static readonly int _delayOnRetry = 1000;

    #endregion

    #region Other Fields

    private readonly SpyderOptions _options;
    private readonly object _fileLock = new();

    #endregion





    internal FileOperations(
        SpyderOptions options)
        {
            _options = options;
            Directory.CreateDirectory(_options.LogPath);

        }





    #region Interface Members

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
        {
        }

    #endregion

    #region Public Methods

    public ConcurrentDictionary<string, string> LoadCacheIndex()
        {
            var path = Path.Combine(_options.LogPath, FILENAME);


            if (!File.Exists(path))
                {
                    return new ConcurrentDictionary<string, string>();
                }

            var json = File.ReadAllText(path);
            var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);


            return dict ?? new ConcurrentDictionary<string, string>();
        }





    /// <summary>
    /// Asynchronously writes a specified contents to a specific file.
    /// <param name="contents">
    ///     The contents to be written to the file.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a boolean value indicating if the content was successfully written to the disk.
    /// Returns <c>true</c> if content was successfully written to the disk, <c>false</c> otherwise.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when an error occurs during writing the contents to the file. 
    /// Any exceptions thrown will be handled internally, allowing the method to be 'safe' and always return a task.
    /// </exception>
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SafeFileWriteAsync(
        string path,
        string contents)
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





    public void SaveCacheIndex(
        SpyderOptions                        options,
        ConcurrentDictionary<string, string> concurrentDictionary)
        {
            if (concurrentDictionary is null || concurrentDictionary.IsEmpty)
                {
                    return;
                }

            try
                {
                    var path = Path.Combine(options.LogPath, FILENAME + ".new");
                    var backpath = Path.Combine(options.LogPath, FILENAME + ".bak");
                    var oldpath = Path.Combine(options.LogPath, FILENAME);


                    SafeSerializeAndWrite(path, oldpath, concurrentDictionary);


                }
            catch
                {
                    throw new SpyderException("Failure to save cache index file");
                }

        }

    #endregion

    #region Private Methods

    private void SafeSerializeAndWrite(
        string                               newfile,
        string                               originalfile,
        ConcurrentDictionary<string, string> indexCache)
        {
            ValidateParameters(newfile, originalfile, indexCache);

            lock (_fileLock)
                {
                    WriteToFile(newfile, originalfile, indexCache);
                }
        }





    private void ValidateParameters(
        string                               newfile,
        string                               originalfile,
        ConcurrentDictionary<string, string> indexCache)
        {
            if (string.IsNullOrEmpty(newfile) || string.IsNullOrEmpty(originalfile) || indexCache == null)
                {
                    throw new ArgumentException("Invalid arguments.");
                }
        }





    private void WriteToFile(string newfile, string originalfile, ConcurrentDictionary<string, string> indexCache)
        {
            for (var i = 0; i < _maxRetries; ++i)
                {
                    try
                        {
                            var json = JsonConvert.SerializeObject(indexCache, Formatting.Indented);

                            if (File.Exists(newfile))
                                {
                                    File.Delete(newfile);
                                }

                            if (newfile != null)
                                {
                                    File.WriteAllText(newfile, json);

                                    if (!newfile.Equals(originalfile, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (originalfile != null)
                                                {
                                                    File.Copy(newfile, originalfile, true);
                                                }
                                        }
                                }

                            Log.Trace("Saved Cache Index");


                            break; // When done we can break loop
                        }
                    catch (IOException)
                        {
                            // You may check error code to filter some exceptions out 
                            if (i < _maxRetries - 1) // i is zero-indexed, so we subtract one
                                {
                                    Thread.Sleep(_delayOnRetry); // Wait some time before retrying
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