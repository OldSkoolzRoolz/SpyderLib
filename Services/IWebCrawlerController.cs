using System.ComponentModel;
using System.Windows.Input;



namespace KC.Apps.SpyderLib.Services;

public interface IWebCrawlerController : INotifyPropertyChanged
{
    #region Properteez

    // Members for use in UI
    ICommand CrawlCommand { get; set; }
    bool IsCrawling { get; set; }
    bool IsPaused { get; set; }
    ICommand PauseCommand { get; set; }
    ICommand StopCommand { get; set; }

    #endregion






    #region Public Methods

    public void CancelCrawlingTasks();


    Task StartCrawlingAsync(CancellationToken token);

    #endregion
}