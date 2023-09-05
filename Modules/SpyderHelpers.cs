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
    private static readonly SpyderOptions s_options = SpyderControlService.CrawlerOptions;
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
    public static ConcurrentScrapedUrlCollection FilterScrapedCollection(ConcurrentScrapedUrlCollection collection,
        SpyderOptions                                                                                   spyderOptions)
    {
        return FilterScrapedCollectionCore(collection: collection, options: spyderOptions);
    }





    /// <summary>
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    /// <param name="options"></param>
    private static ConcurrentScrapedUrlCollection FilterScrapedCollectionCore(ConcurrentScrapedUrlCollection collection,
        SpyderOptions                                                                                        options)
    {
        ArgumentNullException.ThrowIfNull(argument: options);
        var baseUri = new Uri(uriString: options.StartingUrl);
        try
        {
            var processedLinks = collection
                                 .Select<KeyValuePair<string, byte>, string>(l => StripQueryFragment(url: l.Key))
                                 .Where(predicate: IsValidUrl)
                                 .Except(s_output.UrlsScrapedThisSession.Select(x => x.Key));

            var internalLinks = new List<string>();
            var externalLinks = new List<string>();
            externalLinks.Capacity = 0;
            internalLinks = processedLinks.Where(link => !IsExternalDomainLinkCore(link: link, baseUri: baseUri))
                                          .ToList();
            externalLinks = processedLinks.Except(second: internalLinks).ToList();

            s_output.CapturedExternalLinks.AddArray(externalLinks.ToArray());
            s_output.CapturedSeedLinks.AddArray(internalLinks.ToArray());
            s_output.UrlsScrapedThisSession.AddArray(processedLinks.ToArray());

            var scrapedUrls = new ConcurrentScrapedUrlCollection();

            // Add new links to target list for scraping next time around.
            if (options.FollowExternalLinks)
            {
                scrapedUrls.AddArray(externalLinks.ToArray());
            }

            if (options.FollowSeedLinks)
            {
                scrapedUrls.AddArray(internalLinks.ToArray());
            }


            return scrapedUrls;
        }
        catch (Exception e)
        {
            // It's advisable to log the error message. Using Console as an example
            Console.Error.WriteLine(value: e);

            // If you can't recover from this exception, it's better to let it bubble up rather than return null.
            throw;
        }
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
        return Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute)
               && Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
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