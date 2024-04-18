using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Properties;



namespace KC.Apps.SpyderLib.Control;

public class OutputControl : IOutputControl
{
    #region feeeldzzz

    private static readonly SpyderOptions s_options = AppContext.GetData("options") as SpyderOptions;

    #endregion






    public OutputControl()
        {
            AppDomain.CurrentDomain.DomainUnload += OnDomainShutdown;
        }






    #region Public Methods

    public void OnLibraryShutdown()
        {
            var collectionDictionary = new Dictionary<ScrapedUrls, string>
                {
                    { this.CapturedVideoLinks, "TestingVideoLinks.txt" },
                    { this.UrlsScrapedThisSession, "AllUrlsCaptured.txt" },
                    { this.FailedCrawlerUrls, "FailedCrawlerUrls.txt" },
                    { this.CapturedExternalLinks, "ExternalLinksTesting.txt" },
                    { this.CapturedSeedLinks, "CapturedSeedUrlsFilename.txt" },
                    { this.CapturedUrlWithSearchResults, "PositiveTagSearchResults.txt" }
                };

            foreach (var entry in collectionDictionary)
                {
                    SaveCollectionToFile(entry.Key, entry.Value);
                }

            Console.WriteLine("Spyder Output written");
        }

    #endregion






    private void OnDomainShutdown(object sender, EventArgs e)
        {
            OnLibraryShutdown();
        }






    #region Private Methods

    private static void SaveCollectionToFile(ScrapedUrls col, string fileName)
        {
            if (col is null)
                {
                    return;
                }

            var path = Path.Combine(Environment.CurrentDirectory, fileName);

            using var fs = new FileStream(path, FileMode.Append);
            using var sw = new StreamWriter(fs);
            foreach (var item in col.AllUrls)
                {
                    sw.WriteLine(item.OriginalString);
                }

            sw.Flush();
        }

    #endregion






    #region Properteez

    /// <summary>
    ///     The urls collected that are NOT on the same host as the <see cref="SpyderOptions.StartingUrl" />
    /// </summary>
    public ScrapedUrls CapturedExternalLinks { get; } = new(s_options.StartingUrl);

    /// <summary>
    ///     The urls collected that are on the same host as the <see cref="SpyderOptions.StartingUrl" />
    /// </summary>
    public ScrapedUrls CapturedSeedLinks { get; } = new(s_options.StartingUrl);

    /// <summary>
    ///     The urls collected that were found to contain  the html tag searched for
    /// </summary>
    public ScrapedUrls CapturedUrlWithSearchResults { get; } = new(s_options.StartingUrl);

    public ScrapedUrls CapturedVideoLinks { get; } = new(s_options.StartingUrl);

    /// <summary>
    ///     Represents the urls of the pages that were scraped for content
    /// </summary>
    public ScrapedUrls CrawledUrls { get; } = new(s_options.StartingUrl);

    /// <summary>
    ///     The collection of urls that failed during the attempt to scrape.
    /// </summary>
    public ScrapedUrls FailedCrawlerUrls { get; } = new(s_options.StartingUrl);

    /// <summary>
    ///     Represents a singleton instance of an OutputControl object.
    /// </summary>
    public static IOutputControl Instance { get; } = new OutputControl();

    /// <summary>
    ///     Represents the urls that have been scraped from the pages that were crawled
    /// </summary>
    public ScrapedUrls UrlsScrapedThisSession { get; } = new(s_options.StartingUrl);

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
    ScrapedUrls CapturedExternalLinks { get; }

    /// <summary>
    ///     Gets or sets the collection of captured seed links
    /// </summary>
    ScrapedUrls CapturedSeedLinks { get; }

    /// <summary>
    ///     Gets or sets the collection of URLs which resulted producing search results
    /// </summary>
    ScrapedUrls CapturedUrlWithSearchResults { get; }

    /// <summary>
    ///     Gets or sets the collection of captured video links
    /// </summary>
    ScrapedUrls CapturedVideoLinks { get; }

    ScrapedUrls CrawledUrls { get; }

    /// <summary>
    ///     Gets or sets the collection of URLs that the web crawler failed to process
    /// </summary>
    ScrapedUrls FailedCrawlerUrls { get; }

    /// <summary>
    ///     Gets or sets the collection of URLs that have been scraped in the current session.
    ///     This collection is used for exclusion purposes.
    /// </summary>
    ScrapedUrls UrlsScrapedThisSession { get; }

    #endregion
}