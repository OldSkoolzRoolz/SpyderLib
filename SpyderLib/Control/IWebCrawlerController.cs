namespace KC.Apps.SpyderLib.Control;

public interface IWebCrawlerController
{
    #region Public Methods

    event EventHandler<CrawlerFinishedEventArgs> CrawlerTasksFinished;








    Task StartCrawlingAsync(int maxDepth, CancellationToken token);

    #endregion




    public void CancelCrawlingTasks();
}