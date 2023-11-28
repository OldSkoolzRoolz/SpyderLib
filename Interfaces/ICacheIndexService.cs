#region

using System.Collections.Concurrent;

using KC.Apps.SpyderLib.Models;

#endregion

namespace KC.Apps.SpyderLib.Interfaces;

public interface ICacheIndexService
{
    #region Public Methods

    ConcurrentDictionary<string, string> CacheIndexItems { get; }
    int CacheItemCount { get; }





    Task<string> GetAndSetContentFromCacheAsync(
        string address);





    void SaveCacheIndex();





    Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address);

    #endregion
}