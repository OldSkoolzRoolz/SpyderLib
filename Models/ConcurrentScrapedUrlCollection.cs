// ReSharper disable All




using System.Collections.Concurrent;



namespace KC.Apps.SpyderLib.Models;

/// <inheritdoc />
public class ConcurrentScrapedUrlCollection : ConcurrentDictionary<string, byte>
{
    #region feeeldzzz

    const byte DefaultValue = 0;

    #endregion






    #region Public Methods

    public void AddRange(IEnumerable<string> itemsToAdd)
    {
        if (itemsToAdd == null) return;

        foreach (var item in itemsToAdd)
        {
            if (!string.IsNullOrEmpty(item) && IsValidUrl(item))
            {
                _ = TryAdd(item, DefaultValue);
            }
        }
    }

    #endregion






    #region Private Methods

    internal void Add(
        string url)
    {
        // weeds out relative urls, only adding absolute urls
        if (IsValidUrl(url))
        {
            _ = TryAdd(url, 0);
        }
    }






    internal void AddArray(
        string[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            // weeds out relative urls, only adding absolute urls
            if (IsValidUrl(array[i]))
            {
                _ = TryAdd(array[i], 0);
            }
        }
    }






    internal void AddArray(
        IEnumerable<KeyValuePair<string, byte>> array)
    {
        foreach (var item in array)
        {
            TryAddIfKeyIsValidUrl(item);
        }
    }






    internal void AddRange(
        ConcurrentDictionary<string, byte> itemsToAdd)
    {
        if (itemsToAdd == null) return;

        var valid = itemsToAdd.Select(url => url.Key)
            .Where(u => !string.IsNullOrEmpty(u) && IsValidUrl(u));

        foreach (var item in valid)
        {
            // weeds out relative urls, only adding absolute urls
            _ = TryAdd(item, 0);
        }
    }






    /// <summary>
    ///     validates a Url as an absolute and formatted correctly
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool IsValidUrl(
        string url)
    {
        return Uri.IsWellFormedUriString(url, UriKind.Absolute) &&
               Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }






    private void TryAddIfKeyIsValidUrl(
        KeyValuePair<string, byte> item)
    {
        if (IsValidUrl(item.Key))
        {
            _ = TryAdd(item.Key, DefaultValue);
        }
    }

    #endregion
}