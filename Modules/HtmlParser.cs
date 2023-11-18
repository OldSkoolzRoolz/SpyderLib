#region

using HtmlAgilityPack;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;



// ReSharper disable All

#endregion

namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Helper methods to aid the parsing of HTML documents
/// </summary>
public static class HtmlParser
{
    #region Public Methods

    /// <summary>
    ///     extracts and sanitizes hyperlinks from the HtmlDocument
    /// </summary>
    /// <param name="docsource"></param>
    /// <returns>Hyperlinks contained in web page</returns>
    public static IEnumerable<string> GetHrefLinksFromDocumentSource(string docsource)
        {
            if (string.IsNullOrEmpty(docsource))
                {
                    return new string[] { };
                }
        

            var doc = CreateHtmlDocument(docsource);


            var links = doc.DocumentNode.SelectNodes("//a[@href]")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(u => !string.IsNullOrEmpty(u));

            OutputControl.Instance.UrlsScrapedThisSession.AddRange(links);

            Console.WriteLine($"raw link count :: {links.Count()}");
            var sanitized = ValidateUrls(links);
            Console.WriteLine($"sanitized raw link count :: {sanitized.Count()}");

            var urlTuple = SeparateUrls(sanitized, new Uri(SpyderControlService.CrawlerOptions.StartingUrl));

            OutputControl.Instance.CapturedExternalLinks.AddRange(urlTuple.OtherUrls);
            OutputControl.Instance.CapturedSeedLinks.AddRange(urlTuple.BaseUrls);

            if (!SpyderControlService.CrawlerOptions.FollowExternalLinks)
                {
                    return urlTuple.BaseUrls;
                }

            return sanitized;
        }





    public static async Task<IEnumerable<HtmlNode>> ExtractVideoNodesFromHtmlContentAsync(string htmlContent)
        {
            var htmlDoc = await Task.Run(() => CreateHtmlDocument(htmlContent));

            return htmlDoc?.DocumentNode.Descendants("video") ?? Enumerable.Empty<HtmlNode>();
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
    public static List<string> ParseNodeCollectionForSources(
        IEnumerable<HtmlNode> collection)
        {
            List<string> sources = new();

            if (collection is null)
                {
                    return sources;
                }

            Parallel.ForEach(
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
    public static List<string> ParseNodeForSourceAttributes(
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





    public static IEnumerable<string> SanitizeUrls(IEnumerable<string> rawUrls)
        {
            var set1 = FilterLocalUrls(rawUrls);

            var set2 = FilterUrlsByScheme(set1);

            var set3 = RemoveDuplicatedUrls(set2);

            return set3;
        }





    private static (List<string> BaseUrls, List<string> OtherUrls) SeparateUrls(IEnumerable<string> sanitizedUrls,
        Uri baseUri)
        {
            var baseUrls = new List<string>();
            var otherUrls = new List<string>();

            foreach (var url in sanitizedUrls)
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            var strippedUrl = uri.GetLeftPart(UriPartial.Path);
                            if (baseUri.IsBaseOf(new Uri(strippedUrl)))
                                baseUrls.Add(strippedUrl);
                            else
                                otherUrls.Add(strippedUrl);
                        }
                    else
                        {
                            //Skip anomaly
                        }
                }


            return (baseUrls, otherUrls);
        }





    public static bool SearchPageForTagName(
        string content,
        string tag)
        {
            var doc = CreateHtmlDocument(content);

            ArgumentNullException.ThrowIfNull(argument: doc);
            ArgumentNullException.ThrowIfNull(tag);
            try
                {
                    var hasTags = doc.DocumentNode.Descendants(tag);
                    if (hasTags.Any())
                        {
                            return true;
                        }
                }
            catch (Exception e)
                {
                    Log.AndContinue(e);
                }


            return false;
        }





    public static bool TryExtractUserTagFromDocument(
        HtmlDocument doc,
        string optionsHtmlTagToSearchFor,
        out ConcurrentScrapedUrlCollection links)
        {
            links = new();
            var tagnodes = doc.DocumentNode.Descendants(optionsHtmlTagToSearchFor);
            var att = ParseNodeCollectionForSources(tagnodes);
            foreach (var link in att)
                {
                    links.Add(link);
                }

            if (links.Any())
                {
                    return true;
                }


            return false;
        }

    #endregion

    #region Private Methods

    private static IEnumerable<string> ValidateUrls(IEnumerable<string> urls)
        {
            var nopatters = FilterPatternsFromUrls(urls);
            var absonly = ToAbsolute(SpyderControlService.CrawlerOptions.StartingUrl, nopatters);


            return absonly.Where(url =>
                {
                    try
                        {
                            return IsValidUrl(url);
                        }
                    catch (Exception)
                        {
                            return false;
                        }
                });
        }





    public static IEnumerable<string> ToAbsolute(string baseUrl, IEnumerable<string> urls)
        {
            Uri baseUri = new Uri(baseUrl);
            List<string> outputUrls = new List<string>();
            foreach (var url in urls)
                {
                    try
                        {
                            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                                {
                                    outputUrls.Add(url);
                                }
                            else
                                {
                                    var newUrl = new Uri(baseUri, url).ToString();
                                    outputUrls.Add(newUrl);
                                }
                        }
                    catch (Exception)
                        {
                            Console.WriteLine($"Failed to convert the url: {url}");
                        }
                }

            return outputUrls;
        }





    /// <summary>
    ///     validates a Url as an absolute and formatted correctly
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool IsValidUrl(
        string url)
        {
            bool success = Uri.IsWellFormedUriString(uriString: url, uriKind: UriKind.Absolute)
                           && Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);


            return success;
        }





    private static IEnumerable<string> FilterUrlsByScheme(IEnumerable<string> urls)
        {
            return urls.Where(url =>
                {
                    try
                        {
                            if (!url.StartsWith("http"))
                                {
                                    return false;
                                }

                            var uri = new Uri(url);


                            return uri.IsWellFormedOriginalString();
                        }
                    catch (UriFormatException)
                        {
                            return false;
                        }
                });
        }





    private static IEnumerable<string> FilterLocalUrls(IEnumerable<string> rawUrls)
        {
            return rawUrls.Where(url =>
                {
                    try
                        {
                            return !url.Contains("#");
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
                            return !patterns.Any(pattern => url.Contains(pattern));
                        }
                    catch (Exception)
                        {
                            return false;
                        }
                });
        }





    private static IEnumerable<string> RemoveDuplicatedUrls(IEnumerable<string> urls)
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(urls));

            //Make sure we don't have any duplicates in our new urls
            if (!OutputControl.Instance.UrlsScrapedThisSession.IsEmpty)
                {
                    var distinctUrls = urls.Except(OutputControl.Instance.UrlsScrapedThisSession.Keys);
                    return distinctUrls;
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
            List<string> temp = new();
            foreach (var node in nodes)
                {
                    var results = ParseNodeForSourceAttributes(node: node);
                    temp.AddRange(collection: results);
                }


            return temp;
        }





    internal static HtmlDocument CreateHtmlDocument(
        string content)
        {
            var doc = new HtmlDocument();
            doc.OptionReadEncoding = true;
            doc.OptionOutputOriginalCase = true;
            doc.DisableServerSideCode = true;


            doc.LoadHtml(content);


            return doc;
        }

    #endregion
}