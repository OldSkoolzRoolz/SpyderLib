#region

// ReSharper disable All
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

using HtmlAgilityPack;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;

#endregion

namespace KC.Apps.SpyderLib;

public static class SpyderHelpers
{
    #region Public Methods

    /// <summary>
    ///     Search <see cref="HtmlDocument" /> for tag identified in Spyder Options
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="url"></param>
    /// <param name="que"></param>
    public static async Task SearchDocForHtmlTagAsync(
        HtmlDocument doc,
        string url,
        IBackgroundDownloadQue que)
        {
            ArgumentNullException.ThrowIfNull(doc);

            if (string.IsNullOrEmpty(url))
                {
                    throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
                }

            ArgumentNullException.ThrowIfNull(que);

            try
                {
                    if (HtmlParser.TryExtractUserTagFromDocument(doc, "video",
                            out var extractedLinks))
                        {
                            //      _outputControl.CapturedVideoLinks.Add(url);



                            foreach (var link in extractedLinks)
                                {
                                    var dl = new DownloadItem(link.Key, "/Data/Spyder/Files");

                                    //  await BuildDownloadTaskAsync(CancellationToken.None, link.Key)

                                    await que.QueueBackgroundWorkItemAsync(dl).ConfigureAwait(false);
                                }
                        }
                }
            catch (SpyderException)
                {
                    Debug.WriteLine($"Error parsing page document {url}");

                    // Log and continue Failed tasks won't hang up the flow. Possible retry?            
                }
        }





    public static Task SearchDocForHtmlTagAsync(HtmlDocument doc, Uri url, IBackgroundDownloadQue que)
        {
            throw new NotImplementedException();
        }

    #endregion

    #region Private Methods

    /*
        /// <summary>
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        /// <param name="options"></param>
        private static ConcurrentScrapedUrlCollection ClassifyScrapedUrlsCore(
            ConcurrentScrapedUrlCollection collection,
            SpyderOptions                  options)
            {
                if (collection == null) throw new ArgumentNullException(nameof(collection));
                if (options == null) throw new ArgumentNullException(nameof(options));

                Uri baseUri = new(options.StartingUrl);
                var sortedlinks = GetBaseUriLinks(collection, baseUri);
                ConcurrentScrapedUrlCollection externalLinks = sortedlinks.External;
                ConcurrentScrapedUrlCollection internals = sortedlinks.Internal;


                OutputControl.CapturedSeedLinks.AddRange(internals);
                OutputControl.CapturedExternalLinks.AddRange(externalLinks);
                OutputControl.UrlsScrapedThisSession.AddRange(collection);
                var results = new ConcurrentScrapedUrlCollection();
                results.AddRange(internals);

                if (options.FollowExternalLinks)
                    {
                        results.AddRange(externalLinks);
                    }


                return results;
            }

    */





    private static bool IsExternalDomainLinkCore(
        string link,
        Uri baseUri)
        {
            _ = Uri.TryCreate(uriString: link, uriKind: UriKind.Absolute, out var uri);
            if (uri == null)
                {
                    throw new ArgumentException(message: "Invalid Uri");
                }


            return !string.Equals(a: uri.Host, b: baseUri.Host, StringComparison.Ordinal);
        }





    /// <summary>
    ///     Load links from given filename previously saved to disk
    ///     <remarks>Will look for file in the OutputFilePath option set in options</remarks>
    /// </summary>
    /// <param name="filename">Filename to load links from</param>
    /// <returns></returns>
    internal static ConcurrentScrapedUrlCollection LoadLinksFromFile(
        string filename)
        {
            string path = "";
            ConcurrentScrapedUrlCollection temp = new();
            try
                {
                    if (!File.Exists(path: path))
                        {
                            return temp;
                        }

                    var file = File.ReadAllLines(path: path);
                    temp.AddArray(array: file);
                }
            catch (Exception e)
                {
                    Debug.WriteLine(value: e);
                    throw;
                }


            return temp;
        }





    /// <summary>
    ///     Strips the query and fragment parts from the given URL.
    /// </summary>
    /// <param name="url">The URL to process.</param>
    /// <returns>
    ///     The URL without the query and fragment parts. If the input URL is null or empty, the return value is the same
    ///     as the input.
    /// </returns>
    /// <remarks>
    ///     This method uses the Uri.GetLeftPart method with UriPartial.Path to strip the query and fragment parts.
    /// </remarks>
    internal static string StripQueryFragment(
        string url)
        {
            if (string.IsNullOrEmpty(value: url))
                {
                    return url;
                }

            var uri = new Uri(uriString: url);
            var x = uri.GetLeftPart(part: UriPartial.Path);


            return x;
        }

    #endregion
}