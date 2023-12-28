using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Services;



namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Represents the public accessible output of a Spyder session.
/// </summary>
public class SpyderOutput : ISpyderOutput
{
    #region Properteez

    public int CacheHits => AbstractCacheIndex.CacheHits;
    public int CacheMisses => AbstractCacheIndex.CacheMisses;
    public int CapturedExt => OutputControl.Instance.CapturedExternalLinks.Count;
    public int CapturedSeeds => OutputControl.Instance.CapturedSeedLinks.Count;
    public int CrawledUrls => WebCrawlerController.CrawledUrlCount;
    public int FailedUrls => OutputControl.Instance.FailedCrawlerUrls.Count;
    public int TotalCacheItems => AbstractCacheIndex.CacheItemCount;
    public int TotalCapturedUrls => OutputControl.Instance.UrlsScrapedThisSession.Count;
    public int TotalFilesDownloaded => QueueProcessingService.DownloadAttempts;
    public TimeSpan TotalSessionTime { get; set; }

    #endregion
}