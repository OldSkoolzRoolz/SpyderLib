#region

using KC.Apps.SpyderLib.Models;

#endregion


namespace KC.Apps.SpyderLib.Interfaces;

internal interface IDownloadControl
{
    #region Public Methods

    void AddDownloadItem(
        DownloadItem item);





    void SetInputComplete();

    #endregion
}