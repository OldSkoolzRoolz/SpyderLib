#region

#endregion

namespace KC.Apps.SpyderLib.Control;

internal class DownloadControl : IDownloadControl
{
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





    // private ILogger<DownloadControl> _logger = SpyderControl.Factory.CreateLogger<DownloadControl>();
}