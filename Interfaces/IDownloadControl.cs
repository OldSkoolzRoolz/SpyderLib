#region

using KC.Apps.SpyderLib.Models;

#endregion

namespace KC.Apps.SpyderLib.Interfaces;

public interface IDownloadControl
{
    #region Public Methods

    Task AddDownloadItem(DownloadItem item);


    Task SearchLocalCacheForHtmlTag();


    void SetInputComplete();

    #endregion
}