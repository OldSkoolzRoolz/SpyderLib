// ReSharper disable All




using HtmlAgilityPack;

using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;



namespace KC.Apps.SpyderLib;

public static class SpyderHelpers
{
    #region Private Methods

    private static bool IsExternalDomainLinkCore(
        string link,
        Uri baseUri)
    {
        _ = Uri.TryCreate(link, UriKind.Absolute, out var uri);
        if (uri == null)
        {
            throw new ArgumentException("Invalid Uri");
        }


        return !string.Equals(uri.Host, baseUri.Host, StringComparison.Ordinal);
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
            if (!File.Exists(path))
            {
                return temp;
            }

            var file = File.ReadAllLines(path);
            temp.AddArray(file);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


        return temp;
    }






    /// <summary>
    ///     Search <see cref="HtmlDocument" /> for tag identified in Spyder Options
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="url"></param>
    /// <param name="que"></param>
    internal static async Task SearchDocForHtmlTagAsync(
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
                foreach (var link in extractedLinks)
                {
                    var dl = new DownloadItem(link.Key, "/Data/Spyder/Files");


                    await que.QueueBackgroundWorkItemAsync(dl).ConfigureAwait(false);
                }
            }
        }
        catch (SpyderException)
        {
            Console.WriteLine($"Error parsing page document {url}");

            // Log and continue Failed tasks won't hang up the flow. Possible retry?            
        }
    }






    internal static Task SearchDocForHtmlTagAsync(HtmlDocument doc, Uri url, IBackgroundDownloadQue que)
    {
        throw new NotImplementedException();
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
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        var uri = new Uri(url);
        var x = uri.GetLeftPart(UriPartial.Path);


        return x;
    }

    #endregion
}