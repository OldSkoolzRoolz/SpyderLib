using System.ComponentModel;



namespace KC.Apps.SpyderLib.Services;

public interface IWebCrawlerController : INotifyPropertyChanged
{
    #region Properteez

    // Members for use in UI
    bool IsCrawling { get; set; }
    bool IsPaused { get; set; }

    #endregion






    #region Public Methods

    public void CancelCrawlingTasks();


    Task StartCrawlingAsync(CancellationToken token);

    #endregion
}