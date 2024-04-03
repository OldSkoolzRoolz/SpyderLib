




using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Windows.Input;

using HtmlAgilityPack;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging;

namespace KC.Apps.SpyderLib.Services;



public sealed class WebCrawlerController : ServiceBase, IWebCrawlerController
{




    #region feeeldzzz

    private readonly ICacheIndexService _cache;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<Task> _crawlerTasks = new();

    private readonly SemaphoreSlim _crawlTasksGate = new(SpyderControlService.CrawlerOptions.ConcurrentCrawlingTasks,
        SpyderControlService.CrawlerOptions.ConcurrentCrawlingTasks);

    private readonly ILogger<WebCrawlerController> _logger;
    private readonly ConcurrentDictionary<string, byte> _visitedUrls = new();
    private ICommand _crawlCommand;
    private int _crawlMethod;
    private Stopwatch _crawlTimer;
    private bool _isCrawling;
    private ICommand _pauseCommand;
    private string _startingHost;
    private ICommand _stopCommand;
    private List<string> _urlsToCrawl = new();
    private HtmlWeb _hapWeb;

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

        _hapWeb = new()
        {
            AutoDetectEncoding = true,
            UserAgent =
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            MaxAutoRedirects = 10,
            UseCookies = true
        };
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

    public ICommand CrawlCommand
    {
        get => _crawlCommand;
        set => SetProperty(ref _crawlCommand, value);
    }



    internal static int CrawledUrlCount { get; }
    public bool IsCrawling { get; set; }
    public bool IsPaused { get; set; }



    public ICommand PauseCommand
    {
        get => _pauseCommand;
        set => SetProperty(ref _pauseCommand, value);
    }



    public static TaskCompletionSource<bool> StartupComplete { get; } = new();



    public ICommand StopCommand
    {
        get => _stopCommand;
        set => SetProperty(ref _stopCommand, value);
    }

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
        _isCrawling = true;
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
            Log.AndContinue(ex, "Crawler aborted unknown error");
        }
        finally
        {
            _crawlTimer.Stop();
            Console.WriteLine($"Elapsed Crawl time {_crawlTimer.ElapsedMilliseconds:000.00}");

            // Fire off the completion event for anyone who is listening
            this.CrawlerTasksFinished?.Invoke(this, new());
        }

    }






    public async Task StartTagSearch(CancellationToken token)
    {
        this.IsCrawling = true;
        var visitedHosts = new HashSet<string>();
        if (token.IsCancellationRequested)
        {
            return;
        }

        _ = _visitedUrls.TryAdd(Options.StartingUrl, 0);

        await ProcessUrlAsync(Options.StartingUrl, token).ConfigureAwait(false);


        while (!_urlsToCrawl.Count.Equals(0) && !token.IsCancellationRequested)
        {
            var url = _urlsToCrawl.First();
            _urlsToCrawl.RemoveAt(0);
            if (url != null && !visitedHosts.Contains(new Uri(url).Host))
            {
                _ = visitedHosts.Add(new Uri(url).Host);

                await ProcessUrlAsync(url, token).ConfigureAwait(false);
            }
        }

        _logger.SpyderTrace("_urlsToCrawl var is empty, Tag search is ending");


        // save captured data to the file output
        OutputControl.Instance.OnLibraryShutdown();
        _logger.SpyderTrace("Saved Tag Search results to file");

        var e = new CrawlerFinishedEventArgs
        { FoundTagsCount = OutputControl.Instance.CapturedUrlWithSearchResults.Count };

        this.CrawlerTasksFinished?.Invoke(null, e);
        _logger.SpyderTrace("Crawler Finisted event fired");
        PrintStats();
    }

    #endregion






    #region Private Methods

    /// <summary>
    ///     Adds specific url data to the output model.
    /// </summary>
    /// <param name="baseUrls">A list of base URLs to be added.</param>
    /// <param name="otherUrls">A list of other URLs to be added.</param>
    private static void AddUrlsToOutputModel(ScrapedUrls scrapeUrls)
    {
        // Add otherUrls array to the CapturedExternalLinks in output object.
        OutputControl.Instance.CapturedExternalLinks.AddRange(scrapeUrls.OtherUrls);


        // Add baseUrls array to the CapturedSeedLinks in output object.
        OutputControl.Instance.CapturedSeedLinks.AddRange(scrapeUrls.BaseUrls);

        // Add both otherUrls and baseUrls arrays to the UrlsScrapedThisSession in the output object.
        OutputControl.Instance.UrlsScrapedThisSession.AddRange(scrapeUrls.AllUrls);


    }






    private static void ParseDocumentLinks(string docSource)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(docSource);

        var links = doc.DocumentNode.SelectNodes("//a[@href]")
            .Select(node => node.Attributes["href"].Value)
            .Select(href => new Uri(new("http://example.com"), href).AbsoluteUri);
    }






    /// <summary>
    /// </summary>
    /// <param name="cleanUrl"></param>
    /// <param name="currentDepth"></param>
    private async Task CrawlAsync(string cleanUrl, int currentDepth)
    {
        _crawlMethod++;
        Console.WriteLine($"method count: {_crawlMethod}");

        _logger.SpyderTrace($"Crawling URL: {cleanUrl}");

        //Throttling semaphores
        await _crawlTasksGate.WaitAsync().ConfigureAwait(false);

        try
        {
            await HandleCrawlUrlAsync(cleanUrl, currentDepth).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce)
        {
            _logger.SpyderInfoMessage(
                $"Crawling task forURL: {cleanUrl} was cancelled.");
        }
        catch (Exception ex)
        {
            //_logger.SpyderInfoMessage($"An unhandled error occurred when crawling URL: {cleanUrl}. Spyder will now exit.");
            Console.WriteLine("Unhandled crawler error occurred.. <WebCrawlerController-340>");
        }
        finally
        {
            _ = _crawlTasksGate.Release();
        }


    }












    //<<<<<<<<<<<<<  ✨ Codeium AI Suggestion  >>>>>>>>>>>>>>
    /// <summary>
    ///     This method handles parsed HTML links in an asynchronous manner.
    /// </summary>
    /// <param name="documentSourceContent">The text content of the source document from which links have to be extracted.</param>
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

        ScrapedUrls scrapedUrls = new(Options.StartingUrl);

        // Check if the URL has already been visited and that the depth limit has not been reached
        if (currentDepth >= Options.LinkDepthLimit || _visitedUrls.ContainsKey(url))
        {
            return;
        }

        // Add the URL to the list of visited URLs
        _ = _visitedUrls.TryAdd(url, 0);



        //Using HAP attempt to get the source html on the url given
        //  var document = await hapWeb.LoadFromWebAsync(url).ConfigureAwait(false);
        var documentsrc = await _cache.GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);


        // Parse the html and extract all hyperlink
        var newPageLinks = HtmlParser.GetHrefLinksFromDocumentSourceProposed(documentsrc, url);

        scrapedUrls.AddRange(newPageLinks);




        var tasks = scrapedUrls.AllUrls.Select(async url =>
            {
                try
                {
                    await CrawlAsync(url.OriginalString, currentDepth + 1).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.AndContinue(ex);
                }
            });


        _crawlerTasks.AddRange(tasks);
    }






    private void OnPostResponse(HttpWebRequest request, HttpWebResponse response)
    {
        var page = response.GetResponseStream();
        var t = page.ToString();
    }






    private void OnDocLoad(HtmlDocument document)
    {
        var links = document.DocumentNode.Descendants("a");

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "");
            if (!string.IsNullOrWhiteSpace(href))
            {
                var absoluteUrl = new Uri(new(link.GetAttributeValue("href", "")), href).AbsoluteUri;
                var parsedUrl = new Uri(absoluteUrl);
                if (parsedUrl.Host == _startingHost)
                {
                    // _urlsToCrawl.Enqueue(absoluteUrl);
                }
            }
        }
    }






    private void OnStopping(object sender, EventArgs e)
    {
        // Log a message notifying that the application domain is being unloaded
        _logger.SpyderInfoMessage(
            "Application domain is being unloaded, stopping all web crawling tasks...");

        // Check if there are tasks still running and take appropriate stop action
        _urlsToCrawl?.Clear();
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
        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "Cache Entries",
            _cache.CacheItemCount, "**");


        Console.WriteLine("**  {0,15}:   {1,28} {2,10}", "urls Crawled", _visitedUrls.Count,
            "**");

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

        if (pageSource.StartsWith("error", StringComparison.Ordinal))
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

        _urlsToCrawl.AddRange(from uri in newurls.AllUrls select url);


    }






    /// <summary>
    ///     Separates a list of sanitized URLs into two lists: base URLs and other URLs.
    /// </summary>
    /// <param name="sanitizedUrls">An <see cref="IEnumerable{T}" /> of sanitized URL strings to be separated.</param>
    /// <param name="baseUri">
    ///     The base URI against which the URLs in 'sanitizedUrls' are compared. URLs that fit this base are
    ///     classified as base URLs.
    /// </param>
    /// <returns>
    ///     A tuple containing two lists of strings. The first list contains the base URLs and the second list contains
    ///     the other URLs.
    /// </returns>
    /// <remarks>
    ///     If the provided URL string cannot be converted into an <see cref="Uri" />, it is logged as an invalid URL and
    ///     skipped.
    /// </remarks>
    private (List<string> BaseUrls, List<string> OtherUrls) SeparateUrls(IEnumerable<string> sanitizedUrls, Uri baseUri)
    {
        var baseUrls = new List<string>();
        var otherUrls = new List<string>();
        foreach (var url in sanitizedUrls)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var strippedUrl = uri.GetLeftPart(UriPartial.Path);
                if (baseUri.IsBaseOf(new(strippedUrl)))
                {
                    baseUrls.Add(strippedUrl);
                }
                else
                {
                    otherUrls.Add(strippedUrl);
                }
            }
            else
            {
                _logger.SpyderInfoMessage($"Skipping Invalid URL: {url}");
            }
        }


        return (baseUrls, otherUrls);
    }






    /// <summary>
    ///     Method serves as a web worker responsible for crawling url
    ///     that are in the _urlsToCrawl variable
    /// </summary>
    private Task StartWorkerAsync(int maxDepth, CancellationToken token)
    {
        Console.WriteLine("Worker starting....");


        while (_urlsToCrawl.Count > 0 && !token.IsCancellationRequested)
        {

            var url = _urlsToCrawl.First();

            var depth = 0;

            _logger.SpyderInfoMessage($"Depth == {depth}  In Que={_urlsToCrawl.Count}");
            _logger.SpyderTrace($"Now crawling url :: {url}");

            // this seems like extra work here. we should prevent adding anything to the que that exceeds the depth limit
            // if crawling this url will exceed the depth limit skip it
            if (depth > maxDepth)
            {
                continue;
            }

            try
            {
                _ = _visitedUrls.TryAdd(url, 9);
            }
            catch (SpyderException e)
            {
                Log.AndContinue(e, "Error adding url to visitedUrls");
            }
        }

        Console.WriteLine("Worker Endings.... *******************************");

        return Task.CompletedTask;
    }


    #endregion


}