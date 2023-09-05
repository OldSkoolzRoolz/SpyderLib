#region

using KC.Apps.Models;

#endregion

namespace KC.Apps.Interfaces;

internal interface IDownloadControl
{
    void AddDownloadItem(DownloadItem item);


    void SetInputComplete();
}