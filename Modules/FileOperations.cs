#region

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using SpyderLib.Properties;



#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#endregion

namespace SpyderLib.Modules;

/// <summary>
/// </summary>
public interface IFileOperations : IDisposable
{
    #region Methods

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    void Dispose();





    string GenerateUniqueFilename();


    ConcurrentDictionary<string, string> LoadCacheIndex();


    string[] LoadLinksFromInputFile(string filename);


    bool SafeFileWrite(string path, string contents);


    void SaveCache(object? state);


    void VerifyCache();

    #endregion
}

public class FileOperations : IDisposable
{
    private const string FILENAME = "Spyder_Cache_Index.json";
    private readonly ConcurrentDictionary<string, string> _cachedDownloads;
    private readonly object _fileLock = new();
    private static readonly object s_lock = new();
    public static ILogger s_logger;
    public static SpyderOptions s_options;





    public FileOperations(ILogger logger, SpyderOptions options)
    {
        s_logger = logger;
        s_options = options;
        // _cachedDownloads = LoadCacheIndex() ?? new ConcurrentDictionary<string, string>();
    }





    public string Name { get; } = "FileOperations";

    #region Methods

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        // TODO release managed resources here
    }





    public string GenerateUniqueFilename()
    {
        string filename;
        do
        {
            filename = Path.GetRandomFileName();
        } while (File.Exists(Path.Combine(path1: s_options.CacheLocation, path2: filename)));

        return filename;
    }





    public void Init()
    {
    }





    public string[] LoadLinksFromInputFile(string filename)
    {
        lock (s_lock)
        {
            try
            {
                return File.ReadAllLines(Path.Combine(path1: s_options.OutputFilePath, path2: s_options.InputFileName));
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
                return Array.Empty<string>();
            }
        }
    }





    public void SaveCache(object? state)
    {
        try
        {
            //SaveCacheIndex();
        }
        catch (Exception e)
        {
            s_logger.LogError(exception: e, message: "Error saving cache");
        }
    }

    #endregion
}