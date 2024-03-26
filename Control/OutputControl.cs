using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Properties;



namespace KC.Apps.SpyderLib.Control;

public class OutputControl : IOutputControl
{
    public OutputControl()
        {
            AppDomain.CurrentDomain.DomainUnload += OnDomainShutdown;
        }






    #region Public Methods

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






    private void OnDomainShutdown(object sender, EventArgs e)
        {
            OnLibraryShutdown();
        }






    #region Private Methods

    private static void SaveCollectionToFile(ConcurrentScrapedUrlCollection col, string fileName)
        {
            if (col is { IsEmpty: true })
                {
                    return;
                }

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






    #region Properteez

    /// <summary>
    ///     The urls collected that are NOT on the same host as the <see cref="SpyderOptions.StartingUrl" />
    /// </summary>
    public ConcurrentScrapedUrlCollection CapturedExternalLinks { get; } = new();

    /// <summary>
    ///     The urls collected that are on the same host as the <see cref="SpyderOptions.StartingUrl" />
    /// </summary>
    public ConcurrentScrapedUrlCollection CapturedSeedLinks { get; } = new();

    /// <summary>
    ///     The urls collected that were found to contain  the html tag searched for
    /// </summary>
    public ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; } = new();

    public ConcurrentScrapedUrlCollection CapturedVideoLinks { get; } = new();

    /// <summary>
    ///     Represents the urls of the pages that were scraped for content
    /// </summary>
    public ConcurrentScrapedUrlCollection CrawledUrls { get; } = new();

    /// <summary>
    ///     The collection of urls that failed during the attempt to scrape.
    /// </summary>
    public ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; } = new();

    /// <summary>
    ///     Represents a singleton instance of an OutputControl object.
    /// </summary>
    public static IOutputControl Instance { get; } = new OutputControl();

    /// <summary>
    ///     Represents the urls that have been scraped from the pages that were crawled
    /// </summary>
    public ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; } = new();

    #endregion
}



/// <summary>
///     Interface for controlling output of web scraping
/// </summary>
public interface IOutputControl
{
    #region Public Methods

    /// <summary>
    ///     Method to be called when the library is shut down
    /// </summary>
    void OnLibraryShutdown();

    #endregion






    #region Properteez

    /// <summary>
    ///     Gets or sets the collection of captured external links
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedExternalLinks { get; }

    /// <summary>
    ///     Gets or sets the collection of captured seed links
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedSeedLinks { get; }

    /// <summary>
    ///     Gets or sets the collection of URLs which resulted producing search results
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; }

    /// <summary>
    ///     Gets or sets the collection of captured video links
    /// </summary>
    ConcurrentScrapedUrlCollection CapturedVideoLinks { get; }

    ConcurrentScrapedUrlCollection CrawledUrls { get; }

    /// <summary>
    ///     Gets or sets the collection of URLs that the web crawler failed to process
    /// </summary>
    ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; }

    /// <summary>
    ///     Gets or sets the collection of URLs that have been scraped in the current session.
    ///     This collection is used for exclusion purposes.
    /// </summary>
    ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; }

    #endregion
}