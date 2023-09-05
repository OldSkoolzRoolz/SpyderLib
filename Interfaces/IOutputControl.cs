#region

using KC.Apps.Models;

#endregion

namespace KC.Apps.Interfaces;

public interface IOutputControl
{
    ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; }
    ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; }
    ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; }
    ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; }
    ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; }
    ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; }


    void OnLibraryShutdown();
}