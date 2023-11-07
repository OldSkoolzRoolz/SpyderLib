#region

using KC.Apps.SpyderLib.Models;

#endregion


namespace KC.Apps.SpyderLib.Services;

public interface ICacheIndexService
{
    #region Public Methods

    Task<PageContent> GetAndSetContentFromCacheAsync(
        string address);





    Task<IEnumerable<string>> GetLinksFromPageContentAsync(string url);




    void SaveCacheIndex();





    Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address);

    #endregion
}