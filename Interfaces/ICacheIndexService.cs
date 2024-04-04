using System.Collections.Concurrent;

using KC.Apps.SpyderLib.Models;



namespace KC.Apps.SpyderLib.Interfaces;

public interface ICacheIndexService
{
 


    int CacheHits { get; }
    int CacheMisses { get; }
    int CacheItemCount { get; }






    #region Public Methods

    Task<string> GetAndSetContentFromCacheAsync(
        string address);











    Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address);

    #endregion
}