#region

using System.Runtime.Serialization.Json;
using System.Text;

#endregion

//Resharper disable all
namespace KC.Apps.SpyderLib.Properties;

internal class SettingsProvider : ISettingsProvider
{
    private static readonly Type[] s_sroKnownTypes = { typeof(string[]) };





    public T LoadSettings<T>(string fileName) where T : class, new()
    {
        if (string.IsNullOrEmpty(value: fileName))
        {
            throw new ArgumentException(message: "String must not be null or empty.", nameof(fileName));
        }

        if (!Path.IsPathRooted(path: fileName))
        {
            throw new ArgumentException(message: "Invalid path. The path must be rooted.", nameof(fileName));
        }

        if (!File.Exists(path: fileName))
        {
            return new();
        }

        using var stream     = new FileStream(path: fileName, mode: FileMode.Open, access: FileAccess.Read);
        var       serializer = new DataContractJsonSerializer(typeof(T), knownTypes: s_sroKnownTypes);
        return serializer.ReadObject(stream: stream) as T ?? new T();
    }





    public void SaveSettings(string fileName, object settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (string.IsNullOrEmpty(value: fileName))
        {
            throw new ArgumentException(message: "String must not be null or empty.", nameof(fileName));
        }

        if (!Path.IsPathRooted(path: fileName))
        {
            throw new ArgumentException(message: "Invalid path. The path must be rooted.", nameof(fileName));
        }

        var directory = Path.GetDirectoryName(path: fileName);
        if (!Directory.Exists(path: directory))
        {
            _ = Directory.CreateDirectory(path: directory);
        }

        using var stream = new FileStream(path: fileName, mode: FileMode.Create, access: FileAccess.Write);
        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                                                                     stream: stream, encoding: Encoding.UTF8, true,
                                                                     true, indentChars: "  "))
        {
            var serializer = new DataContractJsonSerializer(settings.GetType(), knownTypes: s_sroKnownTypes);
            serializer.WriteObject(writer: writer, graph: settings);
            writer.Flush();
        }
    }
}