#region

// ReSharper disable All
using System.Collections.Concurrent;

#endregion

namespace KC.Apps.SpyderLib.Models;

/// <inheritdoc />
public class ConcurrentScrapedUrlCollection : ConcurrentDictionary<string, byte>
{
    const byte DefaultValue = 0;

    #region Public Methods

    public void AddRange(IEnumerable<string> itemsToAdd)
        {
            if (itemsToAdd == null) return;

            foreach (var item in itemsToAdd)
                {
                    if (!string.IsNullOrEmpty(item) && IsValidUrl(item))
                        {
                            TryAdd(item, DefaultValue);
                        }
                }
        }

    #endregion

    #region Private Methods

    internal void Add(
        string url)
        {
            // weeds out relative urls, only adding absolute urls
            if (IsValidUrl(url: url))
                {
                    TryAdd(key: url, 0);
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
                            TryAdd(array[i], 0);
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





    private void TryAddIfKeyIsValidUrl(
        KeyValuePair<string, byte> item)
        {
            if (IsValidUrl(url: item.Key))
                {
                    TryAdd(key: item.Key, DefaultValue);
                }
        }





    internal void AddRange(
        ConcurrentDictionary<string, byte> itemsToAdd)
        {
            if (itemsToAdd == null) return;

            var valid = itemsToAdd.Select(url => url.Key)
                .Where(u => !string.IsNullOrEmpty(value: u) && IsValidUrl(url: u));

            foreach (var item in valid)
                {
                    // weeds out relative urls, only adding absolute urls
                    TryAdd(key: item, 0);
                }
        }





    /// <summary>
    ///     validates a Url as an absolute and formatted correctly
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private bool IsValidUrl(
        string url)
        {
            return Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute)
                   && Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

    #endregion
}