namespace KC.Apps.SpyderLib.Control;

/// <summary>
///     Class WebCrawlerController, implements IWebCrawlerController interface.
/// </summary>
public class CrawlerFinishedEventArgs : EventArgs
{
    #region Properteez

    public int UrlsCrawled { get; set; }

    #endregion
}