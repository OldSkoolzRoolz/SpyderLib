#region

#endregion

namespace KC.Apps.SpyderLib.Control;

internal interface IDownloadControl
{
    void AddDownloadItem(DownloadItem item);


    void SetInputComplete();
}