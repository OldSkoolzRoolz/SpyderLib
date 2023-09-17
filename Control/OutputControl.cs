#region

// ReSharper disable All
using KC.Apps.Interfaces;
using KC.Apps.Models;
using KC.Apps.Properties;

#endregion

namespace KC.Apps.Control;



public class OutputControl : IOutputControl
{
    private static readonly object s_lock = new();

    private readonly SpyderOptions _options;
    // Access the options from SpyderControl





    public OutputControl(SpyderOptions options)
        {
            _options = options;
        }





    // These properties store the captured links
    public ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; } = new();
    public ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; } = new();
    public ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; } = new();
    public ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; } = new();
    public ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; } = new();

    // Internal collection used for exclusion purposes
    public ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; } = new();





    // This method is called when the library is shutting down
    public void OnLibraryShutdown()
        {
            var collectionDictionary = new Dictionary<ConcurrentScrapedUrlCollection, string>
            {
                { this.CapturedVideoLinks, "TestingVideoLinks.txt" },
                {
                    this.CapturedExternalLinks, _options.CaptureExternalLinks
                        ? _options.CapturedExternalLinksFilename
                        : "ExternalLinksTesting.txt"
                }
                ,
                {
                    this.CapturedSeedLinks, _options.CaptureSeedLinks
                        ? _options.CapturedSeedUrlsFilename
                        : "CapturedSeedUrlsFilename.txt"
                }
                , { this.UrlsScrapedThisSession, "AllUrlsCaptured.txt" },
                {
                    this.CapturedUrlWithSearchResults, _options.EnableTagSearch
                        ? "PositiveTagSearchResults.txt"
                        : "ShouldNotSeeThis.txt"
                }
            };

            foreach (var entry in collectionDictionary)
            {
                if (entry.Value is not null)
                {
                    SaveCollectionToFile(col: entry.Key, fileName: entry.Value);
                }
            }

            Console.WriteLine(value: "Output written");
        }





    /// <summary>
    ///     Save the given collection to a file.
    /// </summary>
    /// <param name="col">The collection to save.</param>
    /// <param name="fileName">The name of the file to save to.</param>
    private void SaveCollectionToFile(ConcurrentScrapedUrlCollection col, string fileName)
        {
            if (col is not { IsEmpty: true })
            {
                lock (s_lock)
                {
                    var path = Path.Combine(path1: _options.OutputFilePath, path2: fileName);
                    using var sw = File.CreateText(path: path);
                    foreach (var item in col)
                    {
                        sw.WriteLine(value: item.Key);
                    }
                }
            }
        }
}