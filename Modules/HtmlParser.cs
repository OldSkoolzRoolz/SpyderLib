using CommunityToolkit.Diagnostics;

using HtmlAgilityPack;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;



namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Helper methods to aid the parsing of HTML documents
/// </summary>
internal static class HtmlParser
{
    internal static HtmlDocument CreateHtmlDocument(
        string content)
        {
            Guard.IsNotNull(content);

            var doc = new HtmlDocument
                {
                    DisableServerSideCode = true
                };


            doc.LoadHtml(content);


            return doc;
        }






    /// <summary>
    ///     Filters out URLs that contain any of the specified patterns.
    /// </summary>
    /// <param name="urls">The URLs to filter.</param>
    /// <returns>The filtered URLs.</returns>
    private static IEnumerable<string> FilterPatternsFromUrls(IEnumerable<string> urls)
        {
            var patterns = SpyderControlService.CrawlerOptions.LinkPatternExclusions;
            return patterns is null
                ? urls
                :
                // Returns the urls that do not contain any of the patterns.
                urls.Where(url =>
                    !patterns.Any(pattern =>
                        url.Contains(pattern, StringComparison.CurrentCultureIgnoreCase)));
        }






    /// <summary>
    ///     Filters URLs based on their scheme.
    /// </summary>
    /// <param name="urls">The list of URLs to be filtered.</param>
    /// <returns>An enumerable collection of filtered URLs.</returns>
#pragma warning disable IDE0051 // Remove unused private members -- DO NOT REMOVE
    private static IEnumerable<string> FilterUrlsByScheme(IEnumerable<string> urls)
#pragma warning restore IDE0051 // Remove unused private members
        {
            // It uses the LINQ Where method to filter the input list.
            return urls.Where(url =>
                {
                    // A trycatch block is used to handle potential UriFormatExceptions
                    try
                        {
                            // If the Url doesn't start with the string "http", it is immediately excluded.
                            if (!url.StartsWith("http", StringComparison.Ordinal))
                                {
                                    return false;
                                }

                            // URL is passed to the Uri constructor. The Uri class represents a Uniform Resource Identifier (URI), a compact representation of a resource available to your application on the intranet or internet.
                            var uri = new Uri(url);


                            // Returns true if the Uri is wellformed.
                            return uri.IsWellFormedOriginalString();
                        }
                    // If a UriFormatException is caught (in case the Url string is not a valid Uri), the Url is excluded.
                    catch (UriFormatException)
                        {
                            return false;
                        }
                });
        }






    /// <summary>
    ///     validates a Url as an absolute and formatted correctly
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                {
                    return false;
                }


            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }






    /// <summary>
    ///     The given code is a method named "ParseAttributesFromNodeCollection" that takes an HtmlNodeCollection as input and
    ///     returns a List of strings.
    ///     The method iterates through each node in the collection and retrieves the attributes named "source" and "src" using
    ///     the GetAttributes method of the node. It then adds the values of these attributes to the "sources" list.
    ///     Next, the method checks if there are any child nodes with the tag name "source" or "src". If there are, it
    ///     recursively calls the ParseAttributesFromNodeCollection method on these child nodes to retrieve their attributes as
    ///     well.
    ///     Finally, the method returns the "sources" list containing all the attribute values.
    ///     Note: The code assumes the existence of a method named "GetAttributes" that retrieves the attributes of a node.
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    private static List<string> ParseNodeCollectionForSources(
        IEnumerable<HtmlNode> collection)
        {
            List<string> sources = new();

            if (collection is null)
                {
                    return sources;
                }

            _ = Parallel.ForEach(
                collection, node =>
                    {
                        // this sometimes returns null elements in the ienumerable
                        var attr = node.GetAttributes("src", "source");
                        attr = attr.Where(v => v != null);


                        sources.AddRange(attr.Select(att => att.Value));

                        var descendants = node.Descendants().ToList();
                        var descendantSources = ParseNodeCollectionForSources(descendants);

                        sources.AddRange(descendantSources);
                    });


            return sources.Distinct().ToList();
        }






    /// <summary>
    ///     Method extracts the url from the source or src attrubuteselements and add link to que
    /// </summary>
    /// <param name="node"></param>
    /// <returns>List</returns>
    private static List<string> ParseNodeForSourceAttributes(
        HtmlNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            List<string> urls = new();

            var sourceAttributes = node.GetAttributes("source", "src");
            foreach (var attr in sourceAttributes)
                {
                    if (attr is null)
                        {
                            continue;
                        }

                    var clean = SpyderHelpers.StripQueryFragment(attr.Value);
                    urls.Add(clean);
                }


            return urls;
        }






    private static (List<string> BaseUrls, List<string> OtherUrls) SeparateUrls(
        IEnumerable<string> sanitizedUrls,
        Uri baseUri)
        {
            var baseUrls = new List<string>();
            var otherUrls = new List<string>();

            foreach (var url in sanitizedUrls)
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            var strippedUrl = uri.GetLeftPart(UriPartial.Path);
                            if (baseUri.IsBaseOf(new(strippedUrl)))
                                {
                                    baseUrls.Add(strippedUrl);
                                }
                            else
                                {
                                    otherUrls.Add(strippedUrl);
                                }
                        }
                    //Skip anomaly
                }


            return (baseUrls, otherUrls);
        }






    //<<<<<<<<<<<<<  ✨ Codeium AI Suggestion  >>>>>>>>>>>>>>
    //Verifies and Convert relative urls to absolute if possible
    /// <summary>
    ///     Verifies and converts relative URLs to absolute if possible.
    /// </summary>
    /// <param name="baseUrl">The base URL to resolve relative URLs against.</param>
    /// <param name="urls">The collection of URLs to convert.</param>
    /// <returns>A list of absolute URLs.</returns>
    private static List<string> ToAbsoluteUrls(string baseUrl, IEnumerable<string> urls)
        {
            // Create a new Uri object from the base URL
            var baseUri = new Uri(baseUrl);

            // Create a list to store the output URLs
            var outputUrls = new List<string>();

            // Iterate over each URL in the input collection
            foreach (var url in urls)
                {
                    // Skip invalid URLs
                    if (!IsValidUrl(url))
                        {
                            continue;
                        }

                    try
                        {
                            // If the URL is already an absolute URL, use it as is
                            // Otherwise, resolve the URL relative to the base URL
                            var newUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute)
                                ? url
                                : new Uri(baseUri, url).ToString();

                            // Add the new URL to the output list
                            outputUrls.Add(newUrl);
                        }
                    catch (InvalidOperationException)
                        {
                            Console.WriteLine($"Failed to convert the url: {url}");
                            // If an exception occurs while converting the URL, print an error message
                            Console.WriteLine($"Failed to convert the URL: {url}");
                        }
                }

            // Return the list of output URLs
            return outputUrls;
        }






    #region Public Methods

    public static List<string> GetHrefLinksFromDocumentSourceProposed(string webPagesource, string crawledUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(webPagesource);

            var tmeplinks = new List<string>();
            try
                {
                    tmeplinks = doc.DocumentNode.SelectNodes("//a[@href]")
                        ?.Select(lnk => lnk.GetAttributeValue("href", ""))
                        .Where(node => !string.IsNullOrWhiteSpace(node))
                        ?.Select(rellnk => new Uri(new(crawledUrl), rellnk).AbsoluteUri)
                        .Where(link => !string.IsNullOrWhiteSpace(link) && IsValidUrl(link))
                        ?.ToList();
                }
            catch (Exception e)
                {
                    Log.AndContinue(e, "Error in GetHrefLinksFromDocumentSourceProposed");
                }


            return tmeplinks;
        }






    /// <summary>
    ///     This method is used to extract all hyperlinks from the given html source. It parses the HTML,
    ///     extracts 'href' values, filters out invalid URLs, and then categorizes them into two groups
    ///     extracts 'href' values, filters out invalid URLs, and then categorizes them into two groups
    ///     external links and links of the same domain. It also updates the URL count on the OutputControl.
    ///     Decides to either return only base URLs or both types based on a configuration flag.
    /// </summary>
    /// <param name="webPagesource">The source html from which links need to be extracted.</param>
    /// <returns>
    ///     Returns tuple of seed urls and external urls.
    /// </returns>
    public static ScrapedUrls GetHrefLinksFromDocumentSource(HtmlDocument doc)
        {
            ScrapedUrls pageUrls = new(ServiceBase.Options.StartingUrl);

            try
                {
                    //Extract all html nodes containing links
                    var links = doc.DocumentNode.SelectNodes("//a[@href]");

                    if (links.Count == 0)
                        {
                            return default;
                        }


                    //Extract all 'href' values from the links//
                    var pglinks = links.Select(a => a.GetAttributeValue("href", null))
                        .Where(u => !string.IsNullOrEmpty(u)).ToList();
                    pageUrls.AddRange(pglinks);



                    OutputControl.Instance.UrlsScrapedThisSession.AddRange(pageUrls.AllUrls);
                    OutputControl.Instance.CapturedExternalLinks.AddRange(pageUrls.OtherUrls);
                    OutputControl.Instance.CapturedSeedLinks.AddRange(pageUrls.BaseUrls);

                    return pageUrls;
                }
            catch (IOException ex)
                {
                    LoggingMessages.SpyderHelpersException(SpyderControlService.Logger,
                        "Error parsing page source, Resuming...");
                    Console.WriteLine(ex.Message);
                }

            return default;
        }






    public static bool SearchPageForTagName(
        string content,
        string tag)
        {
            var doc = CreateHtmlDocument(content);

            ArgumentNullException.ThrowIfNull(doc);
            ArgumentNullException.ThrowIfNull(tag);
            try
                {
                    var hasTags = doc.DocumentNode.Descendants(tag);
                    if (hasTags.Any())
                        {
                            return true;
                        }
                }
            catch (ArgumentNullException ae)
                {
                    Log.AndContinue(ae);
                }
            catch (Exception e)
                {
                    Log.AndContinue(e);
                    throw;
                }


            return false;
        }






    public static ScrapedUrls TryExtractUserTagFromDocument(
        HtmlDocument doc,
        string tagToSearchFor)
        {
            Guard.IsNotNull(doc);
            Guard.IsNotNull(tagToSearchFor);
           var links = new ScrapedUrls(ServiceBase.Options.StartingUrl);



            var tagnodes = doc.DocumentNode.SelectNodes("//" + tagToSearchFor);
            if (tagnodes is null)
                {
            return links;
                }

            var att = ParseNodeCollectionForSources(tagnodes);
            foreach (var link in att)
                {
                    links.AddUrl(link);
                }

        return links;
        }

    #endregion
}