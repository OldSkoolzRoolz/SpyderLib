#region

// ReSharper disable All
using System.Collections.Concurrent;

#endregion




namespace KC.Apps.SpyderLib.Models;




/// <inheritdoc />
public class ConcurrentScrapedUrlCollection : ConcurrentDictionary<string, byte>
    {
        #region Methods

        internal void Add(string url)
            {
                // weeds out relative urls, only adding absolute urls
                if (IsValidUrl(url: url))
                    {
                        TryAdd(key: url, 0);
                    }
            }





        internal void AddArray(string[] array)
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





        internal void AddArray(IEnumerable<KeyValuePair<string, byte>> array)
            {
                foreach (var item in array)
                    {
                        if (IsValidUrl(url: item.Key))
                            {
                                TryAdd(key: item.Key, 0);
                            }
                    }
            }





        internal void AddRange(ConcurrentDictionary<string, byte> itemsToAdd)
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

        #endregion




        #region Methods

        /// <summary>
        ///     validates a Url as an absolute and formatted correctly
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool IsValidUrl(string url)
            {
                return Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute)
                       && Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult)
                       && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }

        #endregion
    }