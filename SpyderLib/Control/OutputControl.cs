#region

using KC.Apps.SpyderLib.Models;

#endregion


namespace KC.Apps.SpyderLib.Control;

public interface IOutputControl
{
    #region Public Methods

    ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; }

    ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; }

    ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; }

    ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; }

    ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; }

    // Internal collection used for exclusion purposes
    ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; }



    void OnLibraryShutdown();





    #endregion
}

public class OutputControl : IOutputControl
{
    #region Feeelldzz

    private static readonly IOutputControl _instance = new OutputControl();

    #endregion





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

            foreach (var entry in collectionDictionary)
                {
                    if (!entry.Key.IsEmpty)
                        {
                            SaveCollectionToFile(entry.Key, entry.Value);
                        }
                }

            Console.WriteLine("Output written");
        }

    #endregion

    #region Public Methods

    public static IOutputControl Instance => _instance;

    #endregion

    #region Private Methods

    private void SaveCollectionToFile(ConcurrentScrapedUrlCollection col, string fileName)
        {
            if (col is not { IsEmpty: true })
                {

                    var path = Path.Combine(Environment.CurrentDirectory, fileName);

                    using var fs = new FileStream(path, FileMode.Append);
                    using var sw = new StreamWriter(fs);
                    foreach (var item in col)
                        {
                            sw.WriteLine(item.Key);
                        }

                    sw.Flush();
                }
        }

    #endregion
}