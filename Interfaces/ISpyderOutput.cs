namespace KC.Apps.SpyderLib.Interfaces;

public interface ISpyderOutput
{
    #region Properteez

    int CacheHits { get; }
    int CacheMisses { get; }
    int CapturedExt { get; }
    int CapturedSeeds { get; }
    int FailedUrls { get; }
    int TotalCacheItems { get; }
    int TotalCapturedUrls { get; }
    int TotalFilesDownloaded { get; }
    TimeSpan TotalSessionTime { get; set; }

    #endregion
}