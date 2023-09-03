#region

using HtmlAgilityPack;

#endregion



namespace KC.Apps.SpyderLib.Modules;

internal class HtmlParser
{
    internal static ConcurrentScrapedUrlCollection? GetHrefLinksFromDocument(HtmlDocument? doc)
    {
        if (doc is null)
        {
            return null;
        }

        var capturedLinks = new ConcurrentScrapedUrlCollection();

        var nodes = doc.DocumentNode.Descendants(name: "a").ToArray();
        capturedLinks = KC.Apps.SpyderLib.Control.SpyderHelpers.ExtractHyperLinksFromNodes(nodes: nodes);

        return capturedLinks;
    }





    internal static ConcurrentScrapedUrlCollection GetVideoLinksFromDocument(HtmlDocument doc)
    {
        var links = new ConcurrentScrapedUrlCollection();

        try
        {
            var nodes = doc.DocumentNode.Descendants(name: "a").ToArray();
            links = KC.Apps.SpyderLib.Control.SpyderHelpers.ExtractHyperLinksFromNodes(nodes: nodes);
        }
        catch (Exception)
        {
            // Swallow any exception can safely return to caller
        }

        return links;
    }





    /// <summary>
    ///     Method extracts the url from the source or src attrubuteselements and add link to que
    /// </summary>
    /// <param name="node"></param>
    /// <returns>List</returns>
    internal List<string> ParseNodeForSourceAttributes(HtmlNode node)
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

                var clean = KC.Apps.SpyderLib.Control.SpyderHelpers.StripQueryFragment(url: attr.Value);
                urls.Add(item: clean);
            }
        }
        catch (Exception e)
        {
            //Swallow
        }

        return urls;
    }





    /// <summary>
    ///     Iteration Wrapper overload for single node method
    /// </summary>
    /// <param name="nodes">IEnumerable</param>
    internal List<string> ParseNodesForVideoSourceAttributes(IEnumerable<HtmlNode> nodes)
    {
        List<string> temp = new();
        foreach (var node in nodes)
        {
            var results = ParseNodeForSourceAttributes(node: node);
            temp.AddRange(collection: results);
        }

        return temp;
    }





    internal static bool SearchPageForTagName(HtmlDocument htmlDocument, string tag)
    {
        ArgumentNullException.ThrowIfNull(argument: htmlDocument);

        try
        {
            var hasTags = htmlDocument.DocumentNode.Descendants(name: tag);
            if (hasTags.Any())
            {
                return true;
            }
        }
        catch (Exception e)
        {
            //Swallow return collection so far if is in valid state.
        }

        return false;
    }
}