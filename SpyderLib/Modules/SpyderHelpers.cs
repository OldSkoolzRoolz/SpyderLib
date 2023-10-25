#region

// ReSharper disable All
using System.Diagnostics.Eventing.Reader;
using HtmlAgilityPack;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;

#endregion


namespace KC.Apps.SpyderLib;

/// <summary>
/// </summary>
public class SpyderHelpers
{
    /// <summary>
    ///     Method organizes and cleans the links in the collection and filters out links according to SpyderOptions
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="spyderOptions"></param>
    /// <returns></returns>
    public static ConcurrentScrapedUrlCollection ClassifyScrapedUrls(
        ConcurrentScrapedUrlCollection collection,
        SpyderOptions                  spyderOptions)
        {
            return ClassifyScrapedUrlsCore(collection: collection, options: spyderOptions);
        }





    /// <summary>
    ///     Process captured nodes and extracts Anchor Links
    ///     Method currently optimized using linq and filters external links(links not in starting domain)
    /// </summary>
    /// <param name="nodes">A collection of HtmlNodes containing Anchor links.</param>
    /// <returns>A collection of valid links</returns>
    public static ConcurrentScrapedUrlCollection ExtractHyperLinksFromNodes(IEnumerable<HtmlNode> nodes)
        {
            var htmlNodes = nodes as HtmlNode[] ?? nodes.ToArray();

            //original method
            var validUrls = htmlNodes
                            .Select(node => node.GetAttributeValue(name: "href", def: string.Empty))
                            .Where(link => !string.IsNullOrEmpty(value: link) && IsValidUrl(url: link))
                            .ToArray();

            ConcurrentScrapedUrlCollection scrapedUrls = new();
            scrapedUrls.AddArray(array: validUrls);
            return scrapedUrls;
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





    /// <summary>
    ///     Checks if the link is outside the domain of the starting url
    ///     Convenient for keeping irrelevent links from the collections.
    ///     Can be initiated in the SpyderOptions
    /// </summary>
    /// <param name="urlLink"></param>
    /// <param name="startingurl"></param>
    /// <returns></returns>
    public static bool IsExternalDomainLink(string urlLink, string startingurl)
        {
            var baseUri = new Uri(startingurl);
            return IsExternalDomainLinkCore(link: urlLink, baseUri: baseUri);
        }





    /// <summary>
    ///     validates a Url as an absolute and formatted correctly
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsValidUrl(string url)
        {
            bool success = Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute)
                           && Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return success;
        }





    public static string GenerateFileName(SpyderOptions options)
        {
            string filename;
            do
                {
                    filename = Path.GetRandomFileName();
                } while (File.Exists(Path.Combine(options.OutputFilePath, filename)));

            return filename;
        }





    /// <summary>
    ///     Load links from given filename previously saved to disk
    ///     <remarks>Will look for file in the OutputFilePath option set in options</remarks>
    /// </summary>
    /// <param name="filename">Filename to load links from</param>
    /// <returns></returns>
    internal static ConcurrentScrapedUrlCollection LoadLinksFromFile(string filename)
        {
            string path = ""; //= Path.Combine(path1: s_options.OutputFilePath, path2: filename);
            ConcurrentScrapedUrlCollection temp = new();
            try
                {
                    if (!File.Exists(path: path))
                        {
                            return null;
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
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    internal static string StripQueryFragment(string url)
        {
            if (string.IsNullOrEmpty(value: url))
                {
                    return url;
                }

            var uri = new Uri(uriString: url);
            var x = uri.GetLeftPart(part: UriPartial.Path);
            return x;
        }





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





    private static bool IsExternalDomainLinkCore(string link, Uri baseUri)
        {
            Uri.TryCreate(uriString: link, uriKind: UriKind.Absolute, out var uri);
            if (uri == null)
                {
                    throw new ArgumentException(message: "Invalid Uri");
                }

            return !string.Equals(a: uri.Host, b: baseUri.Host);
        }
}