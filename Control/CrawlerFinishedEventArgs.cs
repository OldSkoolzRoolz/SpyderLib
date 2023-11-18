namespace KC.Apps.SpyderLib.Control;

/// <summary>
///     Class WebCrawlerController, implements IWebCrawlerController interface.
/// </summary>
public class CrawlerFinishedEventArgs : EventArgs
{
    #region Public Methods

    public int UrlsCrawled { get; set; }

    #endregion
}