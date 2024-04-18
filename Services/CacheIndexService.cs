using System.Collections.Concurrent;

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
        LoadCachedIndexAsync().Wait();
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
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Cache Entries", GetCacheItemCount(), "**");


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

    public int CacheHits => _cacheHits;

    public static long CacheItemCount => GetCacheItemCount();






    private static int GetCacheItemCount()
    {
        return s_CachedUrls.Count;
    }






    public int CacheMisses => _cacheMisses;
    public ConcurrentBag<string> CachedUrls => s_CachedUrls;

    #endregion



    protected void OnStartup(object sender, EventArgs eventArgs)
    {
        LoadCachedIndexAsync().Wait();
    }


    #region Public Methods

    protected void OnStopping(object sender, EventArgs eventArgs)
    {
        try
        {
            PrintStats();
        }
        catch (Exception e)
        {
            Log.AndContinue(e);
        }
    }










    private static async Task LoadCachedIndexAsync()
    {


        var tmp = await GetCachedItemsAsync().ConfigureAwait(false);
        foreach (var item in tmp)
        {
            s_CachedUrls.Add(item);
        }


    }












    public async Task<PageContent> GetAndSetContentFromCacheAsync(string address)
    {
        return await GetAndSetContentFromCacheCoreAsync(address).ConfigureAwait(false);

    }






    public Task<PageContent> SetContentCachePublicWrapperAsync(
        string content,
        string address)
    {
        return SetContentCacheAsync(content, address);
    }

    #endregion






    #region Private Methods



    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }











    #endregion
} //namespace