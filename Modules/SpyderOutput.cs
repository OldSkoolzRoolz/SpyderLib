using System.Diagnostics.CodeAnalysis;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Services;



namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Represents the public accessible output of a Spyder session.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class SpyderOutput //: ISpyderOutput
{
    #region Properteez

    public int CapturedExt => OutputControl.Instance.CapturedExternalLinks.Count;
    public int CapturedSeeds => OutputControl.Instance.CapturedSeedLinks.Count;
    public int CrawledUrls => WebCrawlerController.CrawledUrlCount;
    public int FailedUrls => OutputControl.Instance.FailedCrawlerUrls.Count;

    public int TotalCacheItems => (int)CacheIndexService.CacheItemCount;
    public int TotalCapturedUrls => OutputControl.Instance.UrlsScrapedThisSession.Count;
    public int TotalFilesDownloaded => QueueProcessingService.DownloadAttempts;
    public TimeSpan TotalSessionTime { get; set; }

    #endregion
}