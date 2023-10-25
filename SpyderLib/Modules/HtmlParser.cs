#region

using HtmlAgilityPack;
using KC.Apps.Logging;
using KC.Apps.SpyderLib.Models;


// ReSharper disable All

#endregion


namespace KC.Apps.SpyderLib.Modules;

public class HtmlParser
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    protected HtmlParser()
        {
        }





    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public static ConcurrentScrapedUrlCollection GetHrefLinksFromDocument(HtmlDocument doc)
        {
            if (doc is null)
                {
                    return new();
                }

            var nodes = doc.DocumentNode.Descendants(name: "a").ToArray();
            return SpyderHelpers.ExtractHyperLinksFromNodes(nodes: nodes);
        }





    public static IEnumerable<HtmlNode> GetVideoLinksFromDocumentSource(string content)
        {
            var doc = CreateHtmlDocument(content);
            if (doc == null)
                {
                    yield break;
                }

            foreach (var node in doc.DocumentNode.Descendants("video"))
                {
                    yield return node;
                }
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
    public static List<string> ParseNodeCollectionForSources(IEnumerable<HtmlNode> collection)
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

                                     lock (sources)
                                         {
                                             sources.AddRange(attr.Select(att => att.Value));
                                         }

                                     var descendants = node.Descendants().ToList();
                                     var descendantSources = ParseNodeCollectionForSources(descendants);
                                     lock (sources)
                                         {
                                             sources.AddRange(descendantSources);
                                         }
                                 });

            return sources.Distinct().ToList();
        }





    /// <summary>
    ///     Method extracts the url from the source or src attrubuteselements and add link to que
    /// </summary>
    /// <param name="node"></param>
    /// <returns>List</returns>
    public static List<string> ParseNodeForSourceAttributes(HtmlNode node)
        {
            ArgumentNullException.ThrowIfNull(argument: node);
            List<string> urls = new();
            try
                {
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
                }
            catch (Exception)
                {
                    //Swallow
                }

            return urls;
        }





    /// <summary>
    ///     Iteration Wrapper overload for single node method
    /// </summary>
    /// <param name="nodes">IEnumerable</param>
    public static List<string> ParseNodesForVideoSourceAttributes(IEnumerable<HtmlNode> nodes)
        {
            List<string> temp = new();
            foreach (var node in nodes)
                {
                    var results = ParseNodeForSourceAttributes(node: node);
                    temp.AddRange(collection: results);
                }

            return temp;
        }





    public static bool SearchPageForTagName(string content, string tag)
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
        HtmlDocument doc, string optionsHtmlTagToSearchFor, out ConcurrentScrapedUrlCollection links)
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





    internal static HtmlDocument CreateHtmlDocument(string content)
        {
            var doc = new HtmlDocument();
            doc.OptionReadEncoding = true;
            doc.OptionOutputOriginalCase = true;
            doc.DisableServerSideCode = true;


            doc.LoadHtml(content);

            return doc;
        }
}