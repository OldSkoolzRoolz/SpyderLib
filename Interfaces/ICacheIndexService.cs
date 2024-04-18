using System.Collections.Concurrent;

using KC.Apps.SpyderLib.Models;



namespace KC.Apps.SpyderLib.Interfaces;

public interface ICacheIndexService
{
    int CacheHits { get; }
    int CacheMisses { get; }
ConcurrentBag<string> CachedUrls { get; }





    #region Public Methods

    Task<PageContent> GetAndSetContentFromCacheAsync(
        string address);












    Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address);

    #endregion
}