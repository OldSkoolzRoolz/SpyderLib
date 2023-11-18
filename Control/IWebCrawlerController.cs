namespace KC.Apps.SpyderLib.Control;

public interface IWebCrawlerController
{
    #region Public Methods

    public void CancelCrawlingTasks();


    Task StartCrawlingAsync(CancellationToken token);

    #endregion
}