// ReSharper disable UnusedAutoPropertyAccessor.Global




using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;



namespace KC.Apps.SpyderLib.Services;

public sealed class WebCrawlerController : ServiceBase, IWebCrawlerController
{
    private readonly ICacheIndexService _cache;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private ICommand _crawlCommand;
    private readonly CancellationToken _crawlerStopToken;
    private Stopwatch _crawlTimer;
    private readonly ILogger<WebCrawlerController> _logger;
    private readonly SpyderOptions _options;
    private ICommand _pauseCommand;
    private ICommand _stopCommand;

    // private readonly IOutputControl OutputControl.Instance;
    private readonly ConcurrentBag<(string, int)> _urlsToCrawl = new();
    private readonly ConcurrentDictionary<string, bool> _visitedUrls = new();






    /// <summary>
    ///     Constructor for WebCrawlerController.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options">SpyderOptions options object.</param>
    /// <param name="cache">ICacheIndexService cache object.</param>
    public WebCrawlerController(
        ILogger<WebCrawlerController> logger,
        IOptions<SpyderOptions> options,
        ICacheIndexService cache
    )
        {
            ArgumentNullException.ThrowIfNull(argument: options);
            ArgumentNullException.ThrowIfNull(argument: cache);
            _logger = logger;
            _options = options.Value;
            _cache = cache;


            //Listen for host shutdown message to cleanup
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
        }






    #region Properteez

    public ICommand CrawlCommand
        {
            get => _crawlCommand;
            set => SetProperty(field: ref _crawlCommand, value: value);
        }



    internal static int CrawledUrlCount { get; }
    public bool IsCrawling { get; set; }
    public bool IsPaused { get; set; }



    public ICommand PauseCommand
        {
            get => _pauseCommand;
            set => SetProperty(field: ref _pauseCommand, value: value);
        }



    public static TaskCompletionSource<bool> StartupComplete { get; } = new();



    public ICommand StopCommand
        {
            get => _stopCommand;
            set => SetProperty(field: ref _stopCommand, value: value);
        }

    #endregion






    #region Public Methods

    public void CancelCrawlingTasks()
        {
            _logger.SpyderWarning(message: "A request to cancel all Spyder tasks has been initiated");

            // Cancel any active operations
            _cancellationTokenSource.Cancel();
        }






    /// <summary>
    ///     A Public event that can be subscribe to, to be alerted to the completion of the spyder.
    /// </summary>
    public event EventHandler<CrawlerFinishedEventArgs> CrawlerTasksFinished;






    public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }






    /// <summary>
    ///     Sets up some performance counters and begins scraping the first level.
    ///     Also prints out a diagnostic statistics when the Spyder is finished.
    /// </summary>
    /// <param name="token"></param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public async Task StartCrawlingAsync(CancellationToken token)
        {
            _crawlTimer = new();
            _crawlTimer.Start();
            token.ThrowIfCancellationRequested();
            try
                {
                    // Add the starting url to the crawler collection
                    // _urlsToCrawl.Add((_options.StartingUrl, 1));

                    /*
                                        // Spawn numerous tasks(threads) that will serve as crawlers responsible for
                                        // getting page contents and processing the response
                                        await Task.WhenAll(Enumerable.Range(0, _options.ConcurrentCrawlingTasks)
                                                                     .Select(async _ =>
                                                                                 Task.Run(()=> StartWorkerAsync(maxDepth, token)
                                                                                     .ConfigureAwait(false), token)));
                    */
                    Console.WriteLine(value: "Starting crawler");
                    await CrawlAsync(cleanUrl: Options.StartingUrl, 1).ConfigureAwait(false);
                    _crawlTimer.Stop();
                    Debug.WriteLine($"Elapsed Crawl time {_crawlTimer.ElapsedMilliseconds:000.00}");
                    // Fire off the completion event for anyone who is listening
                    this.CrawlerTasksFinished?.Invoke(this, new());
                }
            catch (SpyderException e)
                {
                    _logger.SpyderWebException(message: e.Message);
                    _crawlTimer.Stop();
                }
            finally
                {
                    PrintStats();
                }
        }

    #endregion






    #region Private Methods

    /// <summary>
    ///     Adds specific url data to the output model.
    /// </summary>
    /// <param name="baseUrls">A list of base URLs to be added.</param>
    /// <param name="otherUrls">A list of other URLs to be added.</param>
    private static void AddUrlsToOutputModel(List<string> baseUrls, List<string> otherUrls)
        {
            // Add otherUrls array to the CapturedExternalLinks in output object.
            OutputControl.Instance.CapturedExternalLinks.AddArray(otherUrls.ToArray());

            // Add baseUrls array to the CapturedSeedLinks in output object.
            OutputControl.Instance.CapturedSeedLinks.AddArray(baseUrls.ToArray());

            // Add both otherUrls and baseUrls arrays to the UrlsScrapedThisSession in the output object.
            OutputControl.Instance.UrlsScrapedThisSession.AddArray(otherUrls.ToArray());
            OutputControl.Instance.UrlsScrapedThisSession.AddArray(baseUrls.ToArray());
        }






    private async Task CrawlAsync(string cleanUrl, int currentDepth)
        {
            try
                {
                    await HandleCrawlUrlAsync(url: cleanUrl, currentDepth: currentDepth).ConfigureAwait(false);
                }
            // We don't want exception here as it will cancel the crawl altogether
            // it should be handled downstream so only the one url scrape is affected
            catch (SpyderException)
                {
                    _logger.SpyderInfoMessage(
                        $"An unhandled error occurred when crawling URL: {cleanUrl}. Spyder will now exit.");
                }
        }






    /// <summary>
    ///     Asynchronously handles the crawling of a specific URL.
    /// </summary>
    /// <param name="url">The URL to be crawled.</param>
    /// <param name="currentDepth">The current depth in the link tree.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method retrieves the content from the provided URL if hasn't been crawled this session,
    ///     then, it adds the URL to the visited list to avoid duplicate attempts, and if retrieving the content was
    ///     successful, it parses the content for additional links.
    ///     If the current depth equals the LinkDepthLimit plus one, the method returns without performing any operations.
    /// </remarks>
    private async Task HandleCrawlUrlAsync([NotNull] string url, int currentDepth)
        {
            if (currentDepth == Options.LinkDepthLimit)
                {
                    return;
                }




            var content = await _cache.GetAndSetContentFromCacheAsync(address: url).ConfigureAwait(false);

            //If content retrieval was successful parse for links
            if (string.IsNullOrEmpty(value: content))
                {
                    return;
                }

            // Add to our visited list so we don't duplicate attempts
            _visitedUrls[key: url] = true;

            await HandleParsedHtmlLinksAsync(documentSourceContent: content, currentDepth: currentDepth)
                .ConfigureAwait(false);
        }






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
    private async Task HandleParsedHtmlLinksAsync(string documentSourceContent, int currentDepth)
        {
            if (documentSourceContent.StartsWith(value: "error",
                    comparisonType: StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.GeneralCrawlerError(message: documentSourceContent, null);
                    return;
                }

            var links = HtmlParser.GetHrefLinksFromDocumentSource(webPagesource: documentSourceContent);


            if (links.BaseUrls is null || links.OtherUrls is null)
                {
                    return;
                }

            var newLinks = links.BaseUrls;

            if (Options.FollowExternalLinks)
                {
                    newLinks.AddRange(collection: links.OtherUrls);
                }




            foreach (var link in newLinks.ToArray())
                {
                    await CrawlAsync(cleanUrl: link, currentDepth + 1).ConfigureAwait(false);
                }
        }






    private void OnStopping(object sender, EventArgs e)
        {
            // Log a message notifying that the application domain is being unloaded
            _logger.SpyderInfoMessage(
                message: "Application domain is being unloaded, stopping all web crawling tasks...");

            // Check if there are tasks still running and take appropriate stop action
            if (!_urlsToCrawl.IsEmpty)
                {
                    _urlsToCrawl.Clear();
                    _logger.SpyderInfoMessage(message: "All crawling tasks have been terminated.");
                }




            // Log complete message
            _logger.SpyderInfoMessage(message: "WebCrawlerController has been successfully unloaded.");
        }






    private void PrintStats()
        {
            Console.WriteLine(value: Environment.NewLine);
            Console.WriteLine(value: Environment.NewLine);
            Console.WriteLine(value: "******************************************************");
            Console.WriteLine(value: "**             Spyder Cache Operations              **");
            Console.WriteLine(value: "******************************************************");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Cache Entries",
                arg1: _cache.CacheIndexItems.Count, arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "urls Crawled", arg1: _visitedUrls.Count,
                arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-8}", arg0: "Session Captured",
                arg1: OutputControl.Instance.UrlsScrapedThisSession.Count,
                arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Failed Urls",
                arg1: OutputControl.Instance.FailedCrawlerUrls.Count, arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Seed Urls",
                arg1: OutputControl.Instance.CapturedSeedLinks.Count, arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Ext Urls",
                arg1: OutputControl.Instance.CapturedExternalLinks.Count, arg2: "**");

            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Downloads",
                arg1: QueueProcessingService.DownloadAttempts, arg2: "**");

            Console.WriteLine(value: "**                                                  **");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-9}", arg0: "Cache Hits",
                arg1: AbstractCacheIndex.CacheHits, arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Cache Misses",
                arg1: AbstractCacheIndex.CacheMisses, arg2: "**");
            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Elapsed Time", _crawlTimer.Elapsed.ToString(),
                arg2: "**");


            Console.WriteLine(value: "**                                                  **");
            Console.WriteLine(value: "******************************************************");
        }






    /*




    */
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
    ///     After that, it separates and strips URLs using the 'SeparateAndStripUrls' method by passing the 'BaseHost' and
    ///     fetched URLs. It filters base URLs and external URLs separately.
    ///     In this example, it is assumed that we'll just return an empty array eventually. However, this can be replaced by a
    ///     collection of URLs based on respective requirements.
    /// </remarks>
    private async Task<IEnumerable<string>> ProcessUrlAsync([NotNull] string url, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var page = await _cache.GetAndSetContentFromCacheAsync(address: url).ConfigureAwait(false);
            if (Options.EnableTagSearch || Options.HtmlTagToSearchFor is not null)
                {
                    if (HtmlParser.SearchPageForTagName(content: page, tag: "video"))
                        {
                            OutputControl.Instance!.CapturedUrlWithSearchResults?.Add(url: url);
                        }
                }

            // var urls = HtmlParser.GetHrefLinksFromDocumentSource(page);

            // Log.Debug($"Count togo: {this.UrlsInQueue}");


            // Validate urls and separate urls
            //  var cleanUrls = VerifyAndSeparateUrls(urls: urls, optionsStartingUrl: _options.StartingUrl);
            /*
                    OutputControl.Instance.CapturedExternalLinks.AddArray(cleanUrls.OtherUrls.ToArray());
                    OutputControl.Instance.CapturedSeedLinks.AddArray(cleanUrls.BaseUrls.ToArray());

                    if (_options.FollowExternalLinks)
                    {
                        cleanUrls.BaseUrls.AddRange(collection: cleanUrls.OtherUrls);
                    }
            */

            return default;
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
                    if (Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out var uri))
                        {
                            var strippedUrl = uri.GetLeftPart(part: UriPartial.Path);
                            if (baseUri.IsBaseOf(new(uriString: strippedUrl)))
                                {
                                    baseUrls.Add(item: strippedUrl);
                                }
                            else
                                {
                                    otherUrls.Add(item: strippedUrl);
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
    /* private Task StartWorkerAsync(int maxDepth, CancellationToken token)
     {
         Debug.WriteLine(value: "Worker starting....");


         while (!_urlsToCrawl.IsEmpty && !token.IsCancellationRequested)
         {
             // ReSharper disable once InvertIf
             if (_urlsToCrawl.TryTake(out var result))
             {
                 var url = result.Item1;
                 var depth = result.Item2;

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
                     _ = _visitedUrls.TryAdd(key: url, true);

                     //   var nextUrls = await ProcessUrlAsync(url: url, token: token).ConfigureAwait(false);

                                         foreach (var uriz in nextUrls)
                                         {
                                             _urlsToCrawl.Add((uriz, depth + 1));
                                         }

                 }
                 catch (SpyderException e)
                 {
                     _logger.SpyderError(message: e.Message);
                 }
             }
         }

         // temporary troubleshooting code
         Debug.WriteLine(value: "Worker Endings.... *******************************");
     }
             */
    private (List<string> BaseUrls, List<string> OtherUrls) VerifyAndSeparateUrls(
        IEnumerable<string> urls,
        string optionsStartingUrl)
        {
            _ = Uri.TryCreate(uriString: optionsStartingUrl, uriKind: UriKind.Absolute, out var baseUri);
            var sanitizedUrls = HtmlParser.SanitizeUrls(rawUrls: urls);
            var (baseUrls, otherUrls) = SeparateUrls(sanitizedUrls: sanitizedUrls, baseUri: baseUri);

            AddUrlsToOutputModel(baseUrls: baseUrls, otherUrls: otherUrls);


            return (baseUrls, otherUrls);
        }

    #endregion
}