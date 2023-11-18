#region

using KC.Apps.SpyderLib.Models;

#endregion

namespace KC.Apps.SpyderLib.Control;

/// <summary>
///     Interface for controlling output of web scraping
/// </summary>
public interface IOutputControl
{
    #region Public Methods

    /// <summary>
    ///     Gets or sets the collection of captured external links
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; }

    /// <summary>
    ///     Gets or sets the collection of captured seed links
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; }

    /// <summary>
    ///     Gets or sets the collection of URLs which resulted producing search results
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; }

    /// <summary>
    ///     Gets or sets the collection of captured video links
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; }

    /// <summary>
    ///     Gets or sets the collection of URLs that the web crawler failed to process
    /// </summary>
    ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; }





    /// <summary>
    ///     Method to be called when the library is shut down
    /// </summary>
    void OnLibraryShutdown();





    /// <summary>
    ///     Gets or sets the collection of URLs that have been scraped in the current session.
    ///     This collection is used for exclusion purposes.
    /// </summary>
    ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; }

    #endregion
}

public class OutputControl : IOutputControl
{
    private OutputControl()
        {
        }





    #region Interface Members

    public ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; } = new();
    public ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; } = new();
    public ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; } = new();
    public ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; } = new();
    public ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; } = new();
    public ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; } = new();





    public void OnLibraryShutdown()
        {
            var collectionDictionary = new Dictionary<ConcurrentScrapedUrlCollection, string>
                {
                    { this.CapturedVideoLinks, "TestingVideoLinks.txt" },
                    { this.UrlsScrapedThisSession, "AllUrlsCaptured.txt" },
                    { this.FailedCrawlerUrls, "FailedCrawlerUrls.txt" },
                    { this.CapturedExternalLinks, "ExternalLinksTesting.txt" },
                    { this.CapturedSeedLinks, "CapturedSeedUrlsFilename.txt" },
                    { this.CapturedUrlWithSearchResults, "PositiveTagSearchResults.txt" }
                };

            foreach (var entry in collectionDictionary.Where(entry => !entry.Key.IsEmpty))
                {
                    SaveCollectionToFile(entry.Key, entry.Value);
                }

            Console.WriteLine("Output written");
        }

    #endregion

    #region Public Methods

    public static IOutputControl Instance { get; } = new OutputControl();

    #endregion

    #region Private Methods

    private static void SaveCollectionToFile(ConcurrentScrapedUrlCollection col, string fileName)
        {
            if (col is { IsEmpty: true }) return;
            var path = Path.Combine(Environment.CurrentDirectory, fileName);

            using var fs = new FileStream(path, FileMode.Append);
            using var sw = new StreamWriter(fs);
            foreach (var item in col)
                {
                    sw.WriteLine(item.Key);
                }

            sw.Flush();
        }

    #endregion
}