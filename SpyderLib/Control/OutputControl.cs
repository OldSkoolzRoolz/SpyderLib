#region

using KC.Apps.Properties;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;

#endregion


namespace KC.Apps.SpyderLib.Control;

public static class OutputControl
{
    private static readonly object SLock = new();

    private static SpyderOptions Options => SpyderControlService.CrawlOptions;

    // These properties store the captured links
    internal static ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; } = new();
    internal static ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; } = new();
    public static ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; } = new();
    internal static ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; } = new();
    internal static ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; } = new();

    // Internal collection used for exclusion purposes
    internal static ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; } = new();





    // This method is called when the library is shutting down
    internal static void OnLibraryShutdown()
        {
            var collectionDictionary = new Dictionary<ConcurrentScrapedUrlCollection, string>
                {
                    { CapturedVideoLinks, "TestingVideoLinks.txt" },
                    { UrlsScrapedThisSession, "AllUrlsCaptured.txt" },
                    { FailedCrawlerUrls, "FailedCrawlerUrls.txt" },
                    { CapturedExternalLinks, Options?.CapturedExternalLinksFilename ?? "ExternalLinksTesting.txt" },
                    { CapturedSeedLinks, Options?.CapturedSeedUrlsFilename ?? "CapturedSeedUrlsFilename.txt" },
                    { CapturedUrlWithSearchResults, "PositiveTagSearchResults.txt" }
                };

            foreach (var entry in collectionDictionary)
                {
                    if (!entry.Key.IsEmpty)
                        {
                            SaveCollectionToFile(entry.Key, entry.Value);
                        }
                }

            Console.WriteLine("Output written");
        }





    /// <summary>
    ///     Save the given collection to a file.
    /// </summary>
    /// <param name="col">The collection to save.</param>
    /// <param name="fileName">The name of the file to save to.</param>
    private static void SaveCollectionToFile(ConcurrentScrapedUrlCollection col, string fileName)
        {
            if (col is not { IsEmpty: true })
                {
                    lock (SLock)
                        {
                            if (Options != null)
                                {
                                    var path = Path.Combine(Options.OutputFilePath, fileName);
                                    using var sw = File.CreateText(path);
                                    foreach (var item in col)
                                        {
                                            sw.WriteLine(item.Key);
                                        }
                                }
                        }
                }
        }
}