#region

// ReSharper disable All
using HtmlAgilityPack;

using KC.Apps.Control;
using KC.Apps.Models;
using KC.Apps.Properties;

#endregion

//ReSharper disable All
namespace KC.Apps.Modules;



/// <summary>
/// </summary>
public class SpyderHelpers
{
    private static readonly SpyderOptions s_options =
        SpyderControlService.CrawlerOptions ?? throw new InvalidOperationException();

    private static readonly OutputControl s_output = new(s_options ?? throw new InvalidOperationException());





    protected SpyderHelpers()
        {
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





    /// <summary>
    ///     Method organizes and cleans the links in the collection and filters out links according to SpyderOptions
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="spyderOptions"></param>
    /// <returns></returns>
    public static ConcurrentScrapedUrlCollection FilterScrapedCollection(
        ConcurrentScrapedUrlCollection collection,
        SpyderOptions spyderOptions)
        {
            return FilterScrapedCollectionCore(collection: collection, options: spyderOptions);
        }





    /// <summary>
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    /// <param name="options"></param>
    private static ConcurrentScrapedUrlCollection FilterScrapedCollectionCore(
        ConcurrentScrapedUrlCollection collection,
        SpyderOptions options)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Uri baseUri = new(options.StartingUrl);
            ConcurrentScrapedUrlCollection externalLinks = GetExternalLinks(collection, baseUri);
            ConcurrentScrapedUrlCollection internalLinks = GetInternalLinks(collection, externalLinks);
            SendCapturedLinksToOutput(externalLinks, internalLinks, collection);
            return CreateScrapedUrlCollection(options, externalLinks, internalLinks);
        }





    private static ConcurrentScrapedUrlCollection GetExternalLinks(
        ConcurrentScrapedUrlCollection collection,
        Uri baseUri)
        {
            var items =
                collection.Where(link => IsExternalDomainLinkCore(link.Key, baseUri)) as ConcurrentScrapedUrlCollection;

            return items;
        }





    private static ConcurrentScrapedUrlCollection GetInternalLinks(
        ConcurrentScrapedUrlCollection collection,
        ConcurrentScrapedUrlCollection externalLinks)
        {
            if (externalLinks is null || externalLinks.IsEmpty)
            {
                return collection;
            }

            //Get internal links by filltering out externals
            var internalLinks = collection.Except(externalLinks);

            //Filter already scraped url out from lists;
            var nodupes = internalLinks.Except(s_output.UrlsScrapedThisSession);
            return (ConcurrentScrapedUrlCollection)(nodupes);
        }





    private static void SendCapturedLinksToOutput(
        ConcurrentScrapedUrlCollection externalLinks,
        ConcurrentScrapedUrlCollection internalLinks, ConcurrentScrapedUrlCollection collection)
        {
            s_output?.CapturedExternalLinks.AddRange(externalLinks);
            s_output?.CapturedSeedLinks.AddRange(internalLinks);
            s_output?.UrlsScrapedThisSession.AddRange(collection);
        }





    private static ConcurrentScrapedUrlCollection CreateScrapedUrlCollection(
        SpyderOptions options,
        ConcurrentScrapedUrlCollection externalLinks, ConcurrentScrapedUrlCollection internalLinks)
        {
            var scrapedUrls = new ConcurrentScrapedUrlCollection();
            if (options.FollowExternalLinks)
            {
                scrapedUrls.AddRange(externalLinks);
            }

            if (options.FollowSeedLinks)
            {
                scrapedUrls.AddRange(internalLinks);
            }

            return scrapedUrls;
        }





    public static Dictionary<string, string?> GroupedUrls(int sampleSize, ConcurrentScrapedUrlCollection links)
        {
            var groupedUrls = links
                .Select(dic => new Uri(uriString: dic.Key))
                .GroupBy(uri => uri.Host)
                .ToDictionary(
                    group => group.Key,
                    group => group.Take(count: sampleSize).Select(uri => uri.ToString())
                        .FirstOrDefault());

            return groupedUrls;
        }





    private static Dictionary<string, string?> GroupUrlsByHost(int sampleSize, ConcurrentScrapedUrlCollection links)
        {
            var groupedUrls = links
                .Select(dic => new Uri(uriString: dic.Key))
                .GroupBy(uri => uri.Host)
                .ToDictionary(
                    group => group.Key,
                    group => group.Take(count: sampleSize).Select(uri => uri.ToString())
                        .FirstOrDefault());

            return groupedUrls;
        }





    /// <summary>
    ///     Checks if the link is outside the domain of the starting url
    ///     Convenient for keeping irrelevent links from the collections.
    ///     Can be initiated in the SpyderOptions
    /// </summary>
    /// <param name="urlLink"></param>
    /// <returns></returns>
    public static bool IsExternalDomainLink(string urlLink)
        {
            var baseUri = new Uri(uriString: s_options.StartingUrl);
            return IsExternalDomainLinkCore(link: urlLink, baseUri: baseUri);
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





    /// <summary>
    ///     Load links from given filename previously saved to disk
    ///     <remarks>Will look for file in the OutputFilePath option set in options</remarks>
    /// </summary>
    /// <param name="filename">Filename to load links from</param>
    /// <returns></returns>
    internal static ConcurrentScrapedUrlCollection? LoadLinksFromFile(string filename)
        {
            var path = Path.Combine(path1: s_options.OutputFilePath, path2: filename);
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
}