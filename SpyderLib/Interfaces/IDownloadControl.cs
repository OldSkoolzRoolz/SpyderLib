#region

#endregion


#region

using KC.Apps.SpyderLib.Models;

#endregion


namespace KC.Apps.SpyderLib.Interfaces;

internal interface IDownloadControl
{
    void AddDownloadItem(DownloadItem item);




    void SetInputComplete();
}