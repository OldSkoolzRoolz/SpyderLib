using System.Collections.Concurrent;
using System.Diagnostics;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : AbstractCacheIndex, ICacheIndexService, IDisposable
{
    public CacheIndexService(
        SpyderMetrics metrics,
        ILogger<CacheIndexService> logger,
        IMyClient client) : base(client, logger, metrics)
        {
            _logger.SpyderInfoMessage("Cache Index Service Loaded...");
            CacheIndexLoadComplete.TrySetResult(true);
        }






    #region Properteez

    public ConcurrentDictionary<string, string> CacheIndexItems => IndexCache;
    public static TaskCompletionSource<bool> CacheIndexLoadComplete { get; set; } = new();
    public int CacheHits => s_cacheHits;

    /// <summary>
    ///     Cache Items currently in index
    /// </summary>
    public int CacheItemCount => IndexCache.Count;

    public int CacheMisses => s_cacheMisses;

    #endregion






    #region Public Methods

    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }






    public async Task<string> GetAndSetContentFromCacheAsync(string address)
        {
            try
                {
                    Stopwatch timer = new();
                    timer.Start();

                    var content = await GetAndSetContentFromCacheCoreAsync(address)
                        .ConfigureAwait(false);

                    timer.Stop();
                    if (_options.UseMetrics)
                        {
                            _metrics.CrawlElapsedTime(timer.ElapsedMilliseconds);
                        }

                    return content;
                }
            catch (SpyderException)
                {
                    _logger.SpyderWebException("An error occured during a url retrieval.");

                    return "error";
                }
        }






    public Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address)
        {
            return SetContentCacheAsync(content, address);
        }

    #endregion






    #region Private Methods

    protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                {
                    if (disposing)
                        {
                            // unsubscribe from static event
                            SpyderControlService.LibraryHostShuttingDown -= OnStopping;
                        }

                    // Here you can release unmanaged resources if any

                    _disposed = true;
                }
        }






    // Destructor
    ~CacheIndexService()
        {
            Dispose(false);
        }

    #endregion
} //namespace