#region

using KC.Apps.SpyderLib.Models;

#endregion

namespace KC.Apps.SpyderLib.Services;

public interface ICacheIndexService
{
    #region Public Methods

    int CacheItemCount { get; }





    Task<PageContent> GetAndSetContentFromCacheAsync(
        string address);





    void SaveCacheIndex();





    Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address);

    #endregion
}