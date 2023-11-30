#region

using System.Collections.Concurrent;
using System.Diagnostics;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : AbstractCacheIndex, ICacheIndexService, IDisposable
{
    private readonly FileOperations _fileOperations;
    private bool _disposed;
    private Stopwatch _timer;

    #region Interface Members

    public ConcurrentDictionary<string, string> CacheIndexItems => this.IndexCache;

    /// <summary>
    ///     Cache Items currently in index
    /// </summary>
    public int CacheItemCount => this.IndexCache.Count;





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

    #region Public Methods

    public CacheIndexService(SpyderMetrics metrics,
        ILogger<CacheIndexService> logger,
        IOptions<SpyderOptions> options,
        ISpyderClient client)
        {
            Guard.IsNotNull(value: options);
            _metrics = metrics;
            _logger = logger;
            _options = options.Value;
            _client = client;
            _fileOperations = new(options: _options);
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
            this.IndexCache = LoadCacheIndex();
            _logger.SpyderInfoMessage(message: "Cache Index Service Loaded...");
            _ = StartupComplete.TrySetResult(true);
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
                    _fileOperations.Dispose();
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