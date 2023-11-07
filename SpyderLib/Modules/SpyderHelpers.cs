#region

// ReSharper disable All
using System.Diagnostics.Eventing.Reader;

using HtmlAgilityPack;

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;

#endregion


namespace KC.Apps.SpyderLib;


public class SpyderHelpers
{
  





    protected SpyderHelpers()
        {
      
        }





    #region Public Methods

    /// <summary>
    ///     Process captured nodes and extracts Anchor Links
    ///     Method currently optimized using linq and filters external links(links not in starting domain)
    /// </summary>
    /// <param name="nodes">A collection of HtmlNodes containing Anchor links.</param>
    /// <returns>A collection of valid links</returns>
    public static string[] ExtractHyperLinksFromNodes(
        IEnumerable<HtmlNode> nodes)
        {
            var htmlNodes = nodes as HtmlNode[] ?? nodes.ToArray();

            //original method
            var validUrls = htmlNodes
                            .Select(node => node.GetAttributeValue(name: "href", def: string.Empty))
                            .Where(link => !string.IsNullOrEmpty(value: link) && IsValidUrl(url: link))
                            .ToArray();


            return validUrls;
        }





    public static string GenerateFileName(
        SpyderOptions options)
        {
            string filename;
            do
                {
                    filename = Path.GetRandomFileName();
                } while (File.Exists(Path.Combine(options.OutputFilePath, filename)));


            return filename;
        }





    public static (ConcurrentScrapedUrlCollection Internal, ConcurrentScrapedUrlCollection External)
        GetBaseUriLinks(
            ConcurrentScrapedUrlCollection collection,
            Uri                            baseUri)
        {
            var keys = collection.Select(a => a.Key);
            var internals = new ConcurrentScrapedUrlCollection();
            var externals = new ConcurrentScrapedUrlCollection();

            foreach (var item in keys)
                {
                    var k = new Uri(item);
                    if (baseUri.Authority == k.Authority)
                        {
                            internals.TryAdd(item, 1);
                        }
                    else
                        {
                            externals.TryAdd(item, 1);
                        }
                }

            var results = (Internal: internals, External: externals);


            return results;
        }





    public static async Task<IEnumerable<string>> GetLinksFromUrlAsync(
        string            optionsStartingUrl,
        CancellationToken token)
        {
            HtmlWeb web = new HtmlWeb();
            string[] links = new string[] { };

            try
                {
                    var doc = await web.LoadFromWebAsync(optionsStartingUrl);
                    var nodes = doc.DocumentNode.Descendants("a");
                    links = ExtractHyperLinksFromNodes(nodes);
                }
            catch (TaskCanceledException e)
                {
                    Log.AndContinue(e);

                }


            return links;
        }





    /// <summary>
    ///     Checks if the link is outside the domain of the starting url
    ///     Convenient for keeping irrelevent links from the collections.
    ///     Can be initiated in the SpyderOptions
    /// </summary>
    /// <param name="urlLink"></param>
    /// <param name="startingurl"></param>
    /// <returns></returns>
    public static bool IsExternalDomainLink(
        string urlLink,
        string startingurl)
        {
            var baseUri = new Uri(startingurl);


            return IsExternalDomainLinkCore(link: urlLink, baseUri: baseUri);
        }





    /// <summary>
    ///     validates a Url as an absolute and formatted correctly
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsValidUrl(
        string url)
        {
            bool success = Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute)
                           && Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);


            return success;
        }





    /// <summary>
    ///     Search <see cref="HtmlDocument" /> for tag identified in Spyder Options
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="url"></param>
    /// <param name="que"></param>
    public static async Task SearchDocForHtmlTagAsync(
        HtmlDocument           doc,
        string                 url,
        IBackgroundDownloadQue que)
        {
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

                                    await que.QueueBackgroundWorkItemAsync(dl);

                                }
                        }
                }
            catch (Exception)
                {
                    Log.Error($"Eerror parsing page document {url}");

                    // Log and continue Failed tasks won't hang up the flow. Possible retry?            
                }
        }

    #endregion

    #region Private Methods

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
                    Console.WriteLine(value: e);
                }


            return temp;
        }




/// <summary>
/// Strips the query and fragment parts from the given URL.
/// </summary>
/// <param name="url">The URL to process.</param>
/// <returns>The URL without the query and fragment parts. If the input URL is null or empty, the return value is the same as the input.</returns>
/// <remarks>
/// This method uses the Uri.GetLeftPart method with UriPartial.Path to strip the query and fragment parts.
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
        Uri    baseUri)
        {
            Uri.TryCreate(uriString: link, uriKind: UriKind.Absolute, out var uri);
            if (uri == null)
                {
                    throw new ArgumentException(message: "Invalid Uri");
                }


            return !string.Equals(a: uri.Host, b: baseUri.Host);
        }

    #endregion
}