using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;

using HtmlAgilityPack;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public sealed class WebCrawlerController : ServiceBase, IWebCrawlerController, IDisposable
{
    #region feeeldzzz

    private readonly ICacheIndexService _cache;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentBag<Task> _crawlerTasks = new();
    private readonly ILogger<WebCrawlerController> _logger;
    private readonly ScrapedUrls _visitedUrls = new(Options.StartingUrl);
    private Stopwatch _crawlTimer = new();
    private bool _disposedValue;
    private string _startingHost;

    #endregion






    /// <summary>
    ///     Constructor for WebCrawlerController.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="cache">ICacheIndexService cache object.</param>
    public WebCrawlerController(
        ILogger<WebCrawlerController> logger,
        ICacheIndexService cache
    )
    {
        ArgumentNullException.ThrowIfNull(cache);
        _logger = logger;
        _cache = cache;


        //Listen for host shutdown message to cleanup
        SpyderControlService.LibraryHostShuttingDown += OnStopping;
        this.CrawlerTasksFinished += OnCrawlerTasksFinished;
    }






    private void OnCrawlerTasksFinished(object sender, CrawlerFinishedEventArgs e)
    {
        _logger.SpyderInfoMessage($"Crawler Finished. Tags Found {e.FoundTagsCount}");
        _logger.SpyderInfoMessage($"Crawler Finished. Urls Crawled {e.UrlsCrawled}");
        this.IsCrawling = false;
        _cancellationTokenSource.Dispose();
        PrintStats();
    }






    #region Properteez

    internal static int CrawledUrlCount { get; set; }
    public bool IsCrawling { get; set; }
    public bool IsPaused { get; set; }
    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion






    #region Public Methods

    public void CancelCrawlingTasks()
    {
        _logger.SpyderWarning("A request to cancel all Spyder tasks has been initiated");

        // Cancel any active operations
        _cancellationTokenSource.Cancel();
    }






    /// <summary>
    ///     A Public event that can be subscribe to, to be alerted to the completion of the spyder.
    /// </summary>
    public event EventHandler<CrawlerFinishedEventArgs> CrawlerTasksFinished;






    /// <summary>
    ///     Sets up some performance counters and begins scraping the first level.
    ///     Also prints out a diagnostic statistics when the Spyder is finished.
    /// </summary>
    /// <param name="token"></param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public async Task StartCrawlingAsync(CancellationToken token)
    {
        _startingHost = new Uri(Options.StartingUrl).Host;
        //_visitedUrls.AddRange(_cache.CachedUrls);

        _crawlTimer = new();
        _crawlTimer.Start();
        token.ThrowIfCancellationRequested();

        try
        {
            Console.WriteLine("Starting crawler");

            await CrawlAsync(Options.StartingUrl, 1).ConfigureAwait(false);

            // Wait for all the crawlers to finish
            await Task.WhenAll(_crawlerTasks.ToArray()).ConfigureAwait(false);
        }
        catch (SpyderException e)
        {
            _logger.SpyderWebException(e.Message);
        }
        catch (Exception ex)
        {
            Log.AndContinue(ex);
        }
        finally
        {
            _crawlTimer.Stop();
            Console.WriteLine($"Elapsed Crawl time {_crawlTimer.ElapsedMilliseconds:000.00}");

            // Fire off the completion event for anyone who is listening
            this.CrawlerTasksFinished?.Invoke(this, new());
        }
    }






    /// <summary>
    /// </summary>
    /// <param name="cleanUrl"></param>
    /// <param name="currentDepth"></param>
    private async Task CrawlAsync(string cleanUrl, int currentDepth)
    {


#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            await HandleCrawlUrlAsync(cleanUrl, currentDepth).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
        {
            _logger.SpyderInfoMessage(
                $"Crawling task forURL: {cleanUrl} was cancelled.");
        }
        catch (Exception unhandled)
        {
            _logger.SpyderInfoMessage($"An unhandled error occurred when crawling URL: {cleanUrl}.");
            _logger.SpyderError(unhandled.Message);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }






    //<<<<<<<<<<<<<  ✨ Codeium AI Suggestion  >>>>>>>>>>>>>>
    /// <summary>
    ///     This method handles parsed HTML links in an asynchronous manner.
    /// </summary>
    /// <param name="currentDepth">The current depth of the link in the crawling process.</param>
    /// <remarks>
    ///     The method finds all href links in the HTML document using the `HtmlParser.GetHrefLinksFromDocumentSource()`
    ///     method.
    ///     Each found link is then passed to `CrawlAsync` along with a depth parameter incremented by 1.
    ///     The `ConfigureAwait(false)` call is used to allow the continuation to run on any available thread.
    /// </remarks>
    /// <returns>A `Task` that represents the asynchronous operation.</returns>
    private async Task HandleCrawlUrlAsync([NotNull] string url, int currentDepth)
    {
ScrapedUrls scrapeUrls = new(Options.StartingUrl);
        // Check if the URL has already been visited and that the depth limit has not been reached
        if (currentDepth >= Options.LinkDepthLimit)
        {
            
            return;
        }

        // Add the URL to the list of visited URLs
        _visitedUrls.AddUrl(url);

	
	Console.WriteLine($"\n Crawling Url {url}..  depth={currentDepth}... ");


        //attempt to get the source html from the cache if exists or from the web and then set new source cache.
        var page = await _cache.GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);

        if (page.Exception is null)
            {
                // Parse the html and extract all hyperlink
                var newPageLinks = HtmlParser.GetHrefLinksFromDocumentSourceProposed(page.Content, url);

                //Add new urls to our Special collection to automatically filter and sort them.
                scrapeUrls.AddRange(newPageLinks.ToArray());

// Remove any duplicates from the collection
                var sanitized = scrapeUrls.RemoveVisitedUrls(_visitedUrls);

                IEnumerable<Task> tasks;
// Create tasks for each url
                if (!SpyderControlService.CrawlerOptions.FollowExternalLinks)
                    {
                        tasks = sanitized.BaseUrls.Select(async u =>
                            {
                                await CrawlAsync(u.OriginalString, currentDepth + 1).ConfigureAwait(false);
                            });
                    }
                else
                    {
                        tasks = sanitized.AllUrls.Select(async u =>
                            {
                                await CrawlAsync(u.OriginalString, currentDepth + 1).ConfigureAwait(false);
                            });
                    }

                foreach (var t in tasks)
                    {
                        _crawlerTasks.Add(t);
                    }
            }
    }






    private void OnStopping(object sender, EventArgs e)
    {
        PrintStats();
        // Log a message notifying that the application domain is being unloaded
        _logger.SpyderInfoMessage(
            "Application domain is being unloaded, stopping all web crawling tasks...");

        // Check if there are tasks still running and take appropriate stop action
        _logger.SpyderInfoMessage("All crawling tasks have been terminated.");


        _cancellationTokenSource.Dispose();

        // Log complete message
        _logger.SpyderInfoMessage("WebCrawlerController has been successfully unloaded.");
    }






    private void PrintStats()
    {
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine("******************************************************");
        Console.WriteLine("**             Spyder Cache Operations              **");
        Console.WriteLine("******************************************************");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Cache Entries", CacheIndexService.CacheItemCount, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "urls Crawled", _visitedUrls.Count, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,8}", "Session Captured",
            OutputControl.Instance.UrlsScrapedThisSession.Count,
            "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Failed Urls",
            OutputControl.Instance.FailedCrawlerUrls.Count, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Seed Urls",
            OutputControl.Instance.CapturedSeedLinks.Count, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Ext Urls",
            OutputControl.Instance.CapturedExternalLinks.Count, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Downloads", QueueProcessingService.DownloadAttempts,
            "**");
        Console.WriteLine("**                                                  **");
        Console.WriteLine("**  {0,15}:   {1,28} {2,9}", "Cache Hits",
            _cache.CacheHits, "**");
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Cache Misses",
            _cache.CacheMisses, "**");
        Console.WriteLine(
            "**  {0,15}:   {1,28} {2,10}", "Elapsed Time", _crawlTimer.Elapsed.ToString(),
            "**");


        Console.WriteLine("**                                                  **");
        Console.WriteLine("******************************************************");
    }




/*

    /// <summary>
    ///     Asynchronously processes the given URL.
    /// </summary>
    /// <param name="url">The URL to process.</param>
    /// <param name="token"></param>
    /// <returns>
    ///     An asynchronous operation that returns a collection of strings; these strings form a collection of new URLs to
    ///     enqueue.
    /// </returns>
    /// <remarks>
    ///     This method firstly gets links from the page content for the given URL using _cache. Then, it logs the count of
    ///     URLs to crawl.
    /// </remarks>
    private async Task ProcessUrlAsync([NotNull] string url, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var pageSource = await _cache.GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);

        if (pageSource.Content.StartsWith("error", StringComparison.Ordinal))
        {
            return;
        }

        if (Options.EnableTagSearch && Options.HtmlTagToSearchFor is not null)
        {
            if (HtmlParser.SearchPageForTagName(pageSource, "video"))
            {
                //A video tag was found, so add to OutputControl.CapturedUrlWithSearchResults
                OutputControl.Instance!.CapturedUrlWithSearchResults.AddUrl(url);
            }
        }

        HtmlDocument doc = new();
        doc.LoadHtml(pageSource);
        // Scrape the page for links add to var _urlsToCrawl
        var newurls = HtmlParser.GetHrefLinksFromDocumentSource(doc);

        this._urlsToCrawl.AddRange(from uri in newurls.AllUrls select url);
    }

*/




    private ScrapedUrls UrlsToCrawl { get; } = new(Options.StartingUrl);






    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
            }

            _cancellationTokenSource.Dispose();
            _crawlerTasks.Clear();
            _disposedValue = true;
        }
    }






    // ~WebCrawlerController()    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }






    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
