namespace KC.Apps.SpyderLib.Services;

public interface IDownloadControl
{
    #region Public Methods

    Task SearchLocalCacheForHtmlTag();


    void SetInputComplete();

    #endregion
}