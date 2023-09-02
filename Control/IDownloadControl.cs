#region

using SpyderLib.Models;

#endregion

namespace SpyderLib.Control;

internal interface IDownloadControl
{
    #region Methods

    void AddDownloadItem(DownloadItem item);


    void SetInputComplete();

    #endregion
}