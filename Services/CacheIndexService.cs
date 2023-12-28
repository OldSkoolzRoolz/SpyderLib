using System.Collections.Concurrent;
using System.Diagnostics;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : AbstractCacheIndex, ICacheIndexService, IDisposable
{
    private Stopwatch _timer;






    public CacheIndexService(
        SpyderMetrics metrics,
        ILogger<CacheIndexService> logger,
        MyClient client) : base(client: client, logger: logger, metrics: metrics)
        {
            _logger.SpyderInfoMessage(message: "Cache Index Service Loaded...");
            CacheIndexLoadComplete.TrySetResult(true);
        }






    #region Properteez

    public ConcurrentDictionary<string, string> CacheIndexItems => IndexCache;
    public static TaskCompletionSource<bool> CacheIndexLoadComplete { get; set; } = new();

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
                    _timer = new();
                    _timer.Start();

                    var content = await GetAndSetContentFromCacheCoreAsync(address: address)
                        .ConfigureAwait(false);

                    _timer.Stop();
                    if (_options.UseMetrics)
                        {
                            _metrics.CrawlElapsedTime(timing: _timer.ElapsedMilliseconds);
                        }

                    return content;
                }
            catch (SpyderException)
                {
                    _logger.SpyderWebException(message: "An error occured during a url retrieval.");

                    return string.Empty;
                }
        }






    public Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address)
        {
            return SetContentCacheAsync(content: content, address: address);
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