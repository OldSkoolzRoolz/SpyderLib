namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     CrawlerFinishedEventArgs
///     Class is used to pass data to the CrawlerFinished <see langword="event" />
/// </summary>
public class CrawlerFinishedEventArgs
{
    #region Properteez

    public int UrlsCrawled { get; set; }
    public int FoundTagsCount { get; set; }
    public static CrawlerFinishedEventArgs Empty { get; set; }

    #endregion
}