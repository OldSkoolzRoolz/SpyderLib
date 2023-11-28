namespace KC.Apps.SpyderLib.Interfaces;

public interface IWebCrawlerController
{
    #region Public Methods

    public void CancelCrawlingTasks();


    Task StartCrawlingAsync(CancellationToken token);

    #endregion
}