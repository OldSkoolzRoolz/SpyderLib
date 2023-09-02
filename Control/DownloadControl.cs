#region

using SpyderLib.Models;

#endregion

namespace SpyderLib.Control;

internal class DownloadControl : IDownloadControl
{
    #region Methods

    public void AddDownloadItem(DownloadItem item)
    {
        AddItemToQue(item: item);
    }





    private void AddItemToQue(DownloadItem item)
    {
        //_downloader.AddItemToQueue(item: item);
    }





    public void SetInputComplete()
    {
        SetInputCompleteCore();
    }





    private void SetInputCompleteCore()
    {
        //_downloader.Complete();
    }

    #endregion

    // private ILogger<DownloadControl> _logger = SpyderControl.Factory.CreateLogger<DownloadControl>();
}