#region

using System.Diagnostics;

using CommunityToolkit.Diagnostics;

using HtmlAgilityPack;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;

#endregion

namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Helper methods to aid the parsing of HTML documents
/// </summary>
public static class HtmlParser
{
    #region Public Methods

    /// <summary>
    ///     Extracts the 'src' or 'source' attribute values from a given HTML Node collection.
    ///     It only returns the values which are not null or empty and do not start with '/'.
    /// </summary>
    /// <param name="nodeCollection">The HtmlNodeCollection from which to extract the attribute value.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodeCollection is null.</exception>
    /// <returns>A string array of attribute values.</returns>
    public static string[] ExtractVideoNodeLinkSource(HtmlNodeCollection nodeCollection)
        {
            ArgumentNullException.ThrowIfNull(argument: nodeCollection);
            return nodeCollection
                .Select(node =>
                    node.GetAttributeValue(name: "src", node.GetAttributeValue(name: "source", def: string.Empty)))
                .Where(link => !string.IsNullOrEmpty(value: link) && !link.StartsWith('/'))
                .ToArray();
        }





    /// <summary>
    ///     Asynchronously extracts video nodes from a given HTML source.
    /// </summary>
    /// <param name="source">The HTML source string to extract video nodes from.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a collection of video nodes
    ///     extracted from the HTML source.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the source is null or empty.</exception>
    public static async Task<HtmlNodeCollection> ExtractVideoNodesFromDocumentSource(string source)
        {
            Guard.IsNullOrEmpty(text: source);

            // Create HtmlDocument as hinted by "CreateHtmlDocument" method presence
            var document = CreateHtmlDocument(content: source);

            // As we are looking for video nodes, operation might be I/O heavy, hence Task.Run
            return await Task.Run(() =>
                {
                    // assumed "video" to be the relevant node for this context
                    var videoNodes = document.DocumentNode.SelectNodes(xpath: "//video");

                    return videoNodes;
                }).ConfigureAwait(false);
        }





    /// <summary>
    ///     This method is used to extract all hyperlinks from the given html source. It parses the HTML,
    ///     extracts 'href' values, filters out invalid URLs, and then categorizes them into two groups -
    ///     external links and links of the same domain. It also updates the URL count on the OutputControl.
    ///     Decides to either return only base URLs or both types based on a configuration flag.
    /// </summary>
    /// <param name="webPagesource">The source html from which links need to be extracted.</param>
    /// <returns>
    ///     Returns tuple of seed urls and external urls.
    /// </returns>
    public static (List<string> BaseUrls, List<string> OtherUrls) GetHrefLinksFromDocumentSource(string webPagesource)
        {
            Guard.IsNotNullOrWhiteSpace(text: webPagesource);
            if (webPagesource.StartsWith(value: "Error", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    return default;
                }

            var doc = CreateHtmlDocument(content: webPagesource);

            try
                {
                    var links = doc.DocumentNode.SelectNodes("//a[@href]")
                        .Select(a => a.GetAttributeValue(name: "href", null))
                        .Where(u => !string.IsNullOrEmpty(value: u));

                    var rawLinks = links as string[] ?? links.ToArray();

                    Debug.WriteLine($"raw link count :: {rawLinks.Length}");
                    var urlTuple = SeparateUrls(ValidateUrls(urls: rawLinks),
                        new(uriString: SpyderControlService.CrawlerOptions.StartingUrl));


                    OutputControl.Instance.UrlsScrapedThisSession.AddRange(itemsToAdd: rawLinks);
                    OutputControl.Instance.CapturedExternalLinks.AddRange(itemsToAdd: urlTuple.OtherUrls);
                    OutputControl.Instance.CapturedSeedLinks.AddRange(itemsToAdd: urlTuple.BaseUrls);

                    return urlTuple;
                }
            catch (Exception)
                {
                    LoggingMessages.SpyderHelpersException(logger: SpyderControlService.Logger,
                        message: "Error parsing page source, Resuming...");
                    throw new SpyderException(message: "Error parsing page source");
                }
        }





    public static IEnumerable<string> SanitizeUrls(IEnumerable<string> rawUrls)
        {
            var set1 = FilterLocalUrls(rawUrls: rawUrls);

            var set2 = FilterUrlsByScheme(urls: set1);

            var set3 = RemoveDuplicatedUrls(urls: set2);

            return set3;
        }





    public static bool SearchPageForTagName(
        string content,
        string tag)
        {
            var doc = CreateHtmlDocument(content: content);

            ArgumentNullException.ThrowIfNull(argument: doc);
            ArgumentNullException.ThrowIfNull(argument: tag);
            try
                {
                    var hasTags = doc.DocumentNode.Descendants(name: tag);
                    if (hasTags.Any())
                        {
                            return true;
                        }
                }
            catch (ArgumentNullException ae)
                {
                    Log.AndContinue(exception: ae);
                }
            catch (Exception e)
                {
                    Log.AndContinue(exception: e);
                    throw;
                }


            return false;
        }





    public static bool TryExtractUserTagFromDocument(
        HtmlDocument doc,
        string tagToSearchFor,
        out ConcurrentScrapedUrlCollection links)
        {
            Guard.IsNotNull(value: doc);
            Guard.IsNotNull(value: tagToSearchFor);
            links = new();
            var tagnodes = doc.DocumentNode.Descendants(name: tagToSearchFor);
            if (tagnodes is null)
                {
                    return false;
                }

            var att = ParseNodeCollectionForSources(collection: tagnodes);
            foreach (var link in att)
                {
                    links.Add(url: link);
                }

            return links.Any();
        }

    #endregion

    #region Private Methods

    internal static HtmlDocument CreateHtmlDocument(
        string content)
        {
            Guard.IsNotNull(value: content);

            var doc = new HtmlDocument
                {
                    OptionReadEncoding = true,
                    OptionOutputOriginalCase = true,
                    DisableServerSideCode = true
                };


            doc.LoadHtml(html: content);


            return doc;
        }





    private static IEnumerable<string> FilterLocalUrls(IEnumerable<string> rawUrls)
        {
            var aRawUrls = rawUrls.ToArray();
            Guard.IsNotNull(value: aRawUrls);

            return aRawUrls.Where(url =>
                {
                    try
                        {
                            return !url.Contains('#', comparisonType: StringComparison.Ordinal);
                        }
                    catch (FormatException)
                        {
                            return false;
                        }
                });
        }





    private static IEnumerable<string> FilterPatternsFromUrls(IEnumerable<string> urls)
        {
            var patterns = SpyderControlService.CrawlerOptions.LinkPatternExclusions;
            return urls.Where(url =>
                {
                    try
                        {
                            return !patterns.Any(predicate: url.Contains);
                        }
                    catch (SpyderException)
                        {
                            return false;
                        }
                });
        }





    private static IEnumerable<string> FilterUrlsByScheme(IEnumerable<string> urls)
        {
            return urls.Where(url =>
                {
                    try
                        {
                            if (!url.StartsWith(value: "http", comparisonType: StringComparison.Ordinal))
                                {
                                    return false;
                                }

                            var uri = new Uri(uriString: url);


                            return uri.IsWellFormedOriginalString();
                        }
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
    private static bool IsValidUrl(
        string url)
        {
            Guard.IsNotNull(value: url);
            var success = Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute) &&
                          Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult) &&
                          (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);


            return success;
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
                source: collection, node =>
                    {
                        // this sometimes returns null elements in the ienumerable
                        var attr = node.GetAttributes("src", "source");
                        attr = attr.Where(v => v != null);


                        sources.AddRange(attr.Select(att => att.Value));

                        var descendants = node.Descendants().ToList();
                        var descendantSources = ParseNodeCollectionForSources(collection: descendants);

                        sources.AddRange(collection: descendantSources);
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
            ArgumentNullException.ThrowIfNull(argument: node);
            List<string> urls = new();

            var sourceAttributes = node.GetAttributes("source", "src");
            foreach (var attr in sourceAttributes)
                {
                    if (attr is null)
                        {
                            continue;
                        }

                    var clean = SpyderHelpers.StripQueryFragment(url: attr.Value);
                    urls.Add(item: clean);
                }


            return urls;
        }





    /// <summary>
    ///     Iteration Wrapper overload for single node method
    /// </summary>
    /// <param name="nodes">IEnumerable</param>
    private static List<string> ParseNodesForVideoSourceAttributes(
        IEnumerable<HtmlNode> nodes)
        {
            var htmlNodes = nodes as HtmlNode[] ?? nodes.ToArray();
            Guard.IsNotNull(value: htmlNodes);

            List<string> temp = new();
            foreach (var node in htmlNodes)
                {
                    var results = ParseNodeForSourceAttributes(node: node);
                    if (results.Count > 0)
                        temp.AddRange(collection: results);
                }


            return temp;
        }





    private static IEnumerable<string> RemoveDuplicatedUrls(IEnumerable<string> urls)
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(urls));

            //Make sure we don't have any duplicates in our new urls
            if (!OutputControl.Instance.UrlsScrapedThisSession.IsEmpty)
                {
                    var distinctUrls = urls.Except(second: OutputControl.Instance.UrlsScrapedThisSession.Keys);
                    return distinctUrls;
                }

            return urls;
        }





    private static (List<string> BaseUrls, List<string> OtherUrls) SeparateUrls(IEnumerable<string> sanitizedUrls,
        Uri baseUri)
        {
            var baseUrls = new List<string>();
            var otherUrls = new List<string>();

            foreach (var url in sanitizedUrls)
                {
                    if (Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uri))
                        {
                            var strippedUrl = uri.GetLeftPart(part: UriPartial.Path);
                            if (baseUri.IsBaseOf(new(uriString: strippedUrl)))
                                baseUrls.Add(item: strippedUrl);
                            else
                                otherUrls.Add(item: strippedUrl);
                        }
                    //Skip anomaly
                }


            return (baseUrls, otherUrls);
        }





    private static List<string> ToAbsolute(string baseUrl, IEnumerable<string> urls)
        {
            var baseUri = new Uri(uriString: baseUrl);
            var outputUrls = new List<string>();
            foreach (var url in urls)
                {
                    try
                        {
                            if (Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute))
                                {
                                    outputUrls.Add(item: url);
                                }
                            else
                                {
                                    var newUrl = new Uri(baseUri: baseUri, relativeUri: url).ToString();
                                    outputUrls.Add(item: newUrl);
                                }
                        }
                    catch (SpyderException)
                        {
                            Debug.WriteLine($"Failed to convert the url: {url}");
                        }
                }

            return outputUrls;
        }





    private static IEnumerable<string> ValidateUrls(IEnumerable<string> urls)
        {
            var nopatters = FilterPatternsFromUrls(urls: urls);
            var absonly = ToAbsolute(baseUrl: SpyderControlService.CrawlerOptions.StartingUrl, urls: nopatters);


            return absonly.Where(url =>
                {
                    try
                        {
                            return IsValidUrl(url: url);
                        }
                    catch (SpyderException)
                        {
                            return false;
                        }
                });
        }

    #endregion
}