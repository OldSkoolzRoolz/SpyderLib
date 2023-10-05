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
        public HtmlParser()
            {
            }





        #region Methods

        public static ConcurrentScrapedUrlCollection? GetHrefLinksFromDocument(HtmlDocument? doc)
            {
                if (doc is null)
                    {
                        return null;
                    }

                var capturedLinks = new ConcurrentScrapedUrlCollection();
                var nodes = doc.DocumentNode.Descendants(name: "a").ToArray();
                capturedLinks = SpyderHelpers.ExtractHyperLinksFromNodes(nodes: nodes);
                return capturedLinks;
            }





        public static ConcurrentScrapedUrlCollection GetVideoLinksFromDocument(HtmlDocument doc)
            {
                var links = new ConcurrentScrapedUrlCollection();
                try
                    {
                        var nodes = doc.DocumentNode.Descendants(name: "a").ToArray();
                        links = SpyderHelpers.ExtractHyperLinksFromNodes(nodes: nodes);
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





        public static bool SearchPageForTagName(HtmlDocument htmlDocument, string tag)
            {
                ArgumentNullException.ThrowIfNull(argument: htmlDocument);
                ArgumentNullException.ThrowIfNull(tag);
                try
                    {
                        var hasTags = htmlDocument.DocumentNode.Descendants(tag);
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

        #endregion
    }