// ReSharper disable All



#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#region

using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;

using KC.Apps.Properties;


#endregion

namespace SpyderLib.Modules ;

    /// <summary>
    /// </summary>
    public interface IFileOperations : IDisposable
    {
        string GenerateUniqueFilename();


        ConcurrentDictionary<string, string> LoadCacheIndex();


        string[] LoadLinksFromInputFile(string filename);


        bool SafeFileWrite(string path, string contents);


        void SaveCache(object? state);


        void VerifyCache();
    }




    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    public class FileOperations : IDisposable
    {
        #region Volotiles

        private readonly object _fileLock = new();
        private const string FILENAME = "Spyder_Cache_Index.json";
        private static readonly object s_lock = new();

        #endregion

        public string Name { get; } = "FileOperations";
        public static SpyderOptions? Options { get; set; }

        #region Setup/Teardown

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
            {
                // TODO release managed resources here
            }

        #endregion

        public string GenerateUniqueFilename()
            {
                string filename;
                lock (s_lock)
                {
                    do
                    {
                        filename = Path.GetRandomFileName();
                    } while (File.Exists(Path.Combine(path1: Options.CacheLocation, path2: filename)));
                }
                return filename;
            }





        public string[] LoadLinksFromInputFile(string filename)
            {
                lock (s_lock)
                {
                    try
                    {
                        return
                            File.ReadAllLines(Path.Combine(path1: Options.OutputFilePath, path2: Options.InputFileName));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(value: e);
                        return Array.Empty<string>();
                    }
                }
            }
    }