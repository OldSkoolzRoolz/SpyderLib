using System.Diagnostics;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public class CacheIndexService : AbstractCacheIndex, ICacheIndexService
{
    public CacheIndexService(
        SpyderMetrics metrics,
        ILogger<CacheIndexService> logger,
        IMyClient client) : base(client, logger, metrics)
    {
        SpyderControlService.LibraryHostShuttingDown += OnStopping;

        PrintStats();
        _logger.SpyderInfoMessage("Cache Index Service Loaded...");
    }






    private void PrintStats()
    {
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine("******************************************************");
        Console.WriteLine("**             Spyder Cache Operations              **");
        Console.WriteLine("******************************************************");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Cache Entries", this.CacheItemCount, "**");


        Console.WriteLine("**  {0,15}:   {1,28} {2,8}", "Session Captured",
            OutputControl.Instance.UrlsScrapedThisSession.Count,
            "**");

        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Failed Urls",
            OutputControl.Instance.FailedCrawlerUrls.Count, "**");

        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Seed Urls",
            OutputControl.Instance.CapturedSeedLinks.Count, "**");

        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Ext Urls",
            OutputControl.Instance.CapturedExternalLinks.Count, "**");

        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Downloads",
            QueueProcessingService.DownloadAttempts, "**");

        Console.WriteLine("**                                                  **");
        Console.WriteLine("**  {0,15}:   {1,28} {2,9}", "Cache Hits", this.CacheHits, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Cache Misses", this.CacheMisses, "**");



        Console.WriteLine("**                                                  **");
        Console.WriteLine("******************************************************");
    }











    #region Properteez

    public int CacheHits => s_cacheHits;

    /// <summary>
    ///     Cache Items currently in index
    /// </summary>
    public int CacheItemCount => IndexCache.Count;

    public int CacheMisses => s_cacheMisses;

    #endregion






    #region Public Methods

    protected void OnStopping(object sender, EventArgs eventArgs)
    {
        try
        {
            _logger.SpyderInfoMessage("Cache Index is saved");
            PrintStats();
        }
        catch (SpyderException)
        {
            _logger.SpyderError(
                "Error saving Cache Index. Consider checking data against backup file.");
        }
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

            Console.WriteLine("Cache time {0}ms", timer.ElapsedMilliseconds);



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






    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }






    // Destructor
    ~CacheIndexService()
    {
        Dispose(false);
    }

    #endregion
} //namespace