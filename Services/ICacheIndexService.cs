using System.Collections.Concurrent;

using KC.Apps.SpyderLib.Models;



namespace KC.Apps.SpyderLib.Services;

public interface ICacheIndexService
{
    #region Properteez

    ConcurrentDictionary<string, string> CacheIndexItems { get; }

    #endregion

int CacheHits { get; }

int CacheMisses { get; }
int CacheItemCount { get; }



    #region Public Methods

    Task<string> GetAndSetContentFromCacheAsync(
        string address);






    void SaveCacheIndex();






    Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address);

    #endregion
}