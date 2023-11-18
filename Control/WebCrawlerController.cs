#region

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using KC.Apps.SpyderLib.Extensions;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Control;

public class WebCrawlerController : IWebCrawlerController
{
    private readonly ICacheIndexService _cache;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ILogger<WebCrawlerController> _logger;
    private readonly SpyderOptions _options;

    // private readonly IOutputControl OutputControl.Instance;
    private readonly ConcurrentBag<(string, int)> _urlsToCrawl = new ConcurrentBag<(string, int)>();
    private readonly ConcurrentDictionary<string, bool> _visitedUrls = new();
    private Stopwatch _crawlTimer;

    #region Interface Members

    /// <summary>
    ///     Sets up some performance counters and begins scraping the first level.
    ///     Also prints out a diagnostic statistics when the Spyder is finished.
    /// </summary>
    /// <param name="token"></param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public async Task StartCrawlingAsync(CancellationToken token)
        {
            _crawlTimer = new Stopwatch();
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
                    await CrawlAsync(_options.StartingUrl, 1).ConfigureAwait(false);
                    _crawlTimer.Stop();

                    // Fire off the completion event for anyone who is listening
                    this.CrawlerTasksFinished?.Invoke(this, new CrawlerFinishedEventArgs());
                }
            catch (Exception e)
                {
                    _logger.SpyderWebException(e.Message);
                    _crawlTimer.Stop();
                }
            finally
                {
                    PrintStats();
                }
        }





    /// <summary>
    ///     A Public event that can be subscribe to, to be alerted to the completion of the spyder.
    /// </summary>
    public event EventHandler<CrawlerFinishedEventArgs> CrawlerTasksFinished;





    void IWebCrawlerController.CancelCrawlingTasks()
        {
            Log.Warning("A request to cancel all Spyder tasks has been initiated");

            // Cancel any active operations
            _cancellationTokenSource.Cancel();
        }

    #endregion

    #region Public Methods

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
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(cache);
            _logger = logger;
            _options = options.Value;
            _cache = cache;


            //Listen for host shutdown message to cleanup
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
        }





    private async Task HandleCrawlUrlAsync([NotNull] string url, int currentDepth)
        {
            if (currentDepth == (_options.LinkDepthLimit + 1))
                {
                    return;
                }

            var content = new PageContent(url);

            // If we have not crawled this url this session attempt to retrieve 
            if (!_visitedUrls.ContainsKey(url))
                {
                    content = await _cache.GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);
                }

            //If content retrieval was successful parse for links
            if (!string.IsNullOrEmpty(content.Content) && content.Exception is null)
                {
                    // Add to our visited list so we don't duplicate attempts
                    _visitedUrls[url] = true;

                    await HandleParsedHtmlLinksAsync(content.Content, currentDepth).ConfigureAwait(false);
                }
        }





    private async Task HandleParsedHtmlLinksAsync(string documentSourceContent, int currentDepth)
        {
            var links = HtmlParser.GetHrefLinksFromDocumentSource(documentSourceContent);

            foreach (var link in links)
                {
                    await CrawlAsync(link, currentDepth + 1).ConfigureAwait(false);
                }
        }





    private async Task CrawlAsync(string cleanUrl, int currentDepth)
        {
            Console.WriteLine($"Starting Crawl depth level {currentDepth}");

            try
                {
                    await HandleCrawlUrlAsync(cleanUrl, currentDepth).ConfigureAwait(false);
                }
            // We don't want exception here as it will cancel the crawl altogether
            // it should be handled downstream so only the one url scrape is affected
            catch (Exception e)
                {
                    _logger.LogCritical(
                        $"An unhandled error occurred when crawling URL: {cleanUrl}. Spyder will now exit.");
                    _logger.LogInformation(e.Message);
                }
        }





    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion

    #region Private Methods

    /// <summary>
    ///     Method serves as a web worker responsible for crawling url
    ///     that are in the _urlsToCrawl variable
    /// </summary>
    /// <param name="maxDepth"></param>
    /// <param name="token"></param>
    private async Task StartWorkerAsync(int maxDepth, CancellationToken token)
        {
            Console.WriteLine("Worker starting....");


            while (!_urlsToCrawl.IsEmpty)
                {
                    // ReSharper disable once InvertIf
                    if (_urlsToCrawl.TryTake(out var result))
                        {
                            var url = result.Item1;
                            var depth = result.Item2;

                            _logger.LogInformation($"Depth == {depth}  In Que={_urlsToCrawl.Count}");
                            _logger.LogTrace($"Now crawling url :: {url}");

                            // this seems like extra work here. we should prevent adding anything to the que that exceeds the depth limit
                            // if crawling this url will exceed the depth limit skip it
                            if (depth > maxDepth)
                                {
                                    continue;
                                }

                            try
                                {
                                    _visitedUrls.TryAdd(url, true);

                                    var nextUrls = await ProcessUrlAsync(url, token).ConfigureAwait(false);

                                    foreach (var uriz in nextUrls)
                                        {
                                            _urlsToCrawl.Add((uriz, depth + 1));
                                        }
                                }
                            catch (Exception e)
                                {
                                    Log.Error(e.Message);
                                }
                        }
                }

            // temporary troubleshooting code
            Console.WriteLine("Worker Endings.... *******************************");
        }





    private void OnStopping(object sender, EventArgs e)
        {
            // Log a message notifying that the application domain is being unloaded
            _logger.LogInformation("Application domain is being unloaded, stopping all web crawling tasks...");

            // Check if there are tasks still running and take appropriate stop action
            if (!_urlsToCrawl.IsEmpty)
                {
                    _urlsToCrawl.Clear();
                    _logger.LogInformation("All crawling tasks have been terminated.");
                }

            try
                {
                    //Save dictionary output 
                    //OutputControl.Instance.OnLibraryShutdown();
                }
            catch (Exception exception)
                {
                    _logger.SpyderWebException(exception.Message);
                }
            finally
                {
                    _cancellationTokenSource.Dispose();
                    SpyderControlService.LibraryHostShuttingDown -= OnStopping;
                }


            // Log complete message
            _logger.LogInformation("WebCrawlerController has been successfully unloaded.");
        }





    private void PrintStats()
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("******************************************************");
            Console.WriteLine("**             Spyder Cache Operations              **");
            Console.WriteLine("******************************************************");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Cache Entries", _cache.CacheItemCount, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "urls Crawled", _visitedUrls.Count, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-8}", "Session Captured",
                OutputControl.Instance.UrlsScrapedThisSession.Count,
                "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Failed Urls",
                OutputControl.Instance.FailedCrawlerUrls.Count, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Seed Urls",
                OutputControl.Instance.CapturedSeedLinks.Count, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Ext Urls",
                OutputControl.Instance.CapturedExternalLinks.Count, "**");

            Console.WriteLine("**                                                  **");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-9}", "Cache Hits", CacheIndexService.CacheHits, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Cache Misses", CacheIndexService.CacheMisses, "**");
            Console.WriteLine(
                "**  {0,15}:   {1,-28} {2,-10}", "Elapsed Time", _crawlTimer.Elapsed.ToString(), "**");


            Console.WriteLine("**                                                  **");
            Console.WriteLine("******************************************************");
        }





    /// <summary>
    ///     Adds specific url data to the output model.
    /// </summary>
    /// <param name="baseUrls">A list of base URLs to be added.</param>
    /// <param name="otherUrls">A list of other URLs to be added.</param>
    private void AddUrlsToOutputModel(List<string> baseUrls, List<string> otherUrls)
        {
            // Add otherUrls array to the CapturedExternalLinks in output object.
            OutputControl.Instance.CapturedExternalLinks.AddArray(otherUrls.ToArray());

            // Add baseUrls array to the CapturedSeedLinks in output object.
            OutputControl.Instance.CapturedSeedLinks.AddArray(baseUrls.ToArray());

            // Add both otherUrls and baseUrls arrays to the UrlsScrapedThisSession in the output object.
            OutputControl.Instance.UrlsScrapedThisSession.AddArray(otherUrls.ToArray());
            OutputControl.Instance.UrlsScrapedThisSession.AddArray(baseUrls.ToArray());
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
                            if (baseUri.IsBaseOf(new Uri(strippedUrl)))
                                baseUrls.Add(strippedUrl);
                            else
                                otherUrls.Add(strippedUrl);
                        }
                    else
                        {
                            _logger.LogInformation($"Skipping Invalid URL: {url}");
                        }
                }


            return (baseUrls, otherUrls);
        }





    private (List<string> BaseUrls, List<string> OtherUrls) VerifyAndSeparateUrls(
        IEnumerable<string> urls,
        string optionsStartingUrl)
        {
            _ = Uri.TryCreate(optionsStartingUrl, UriKind.Absolute, out var baseUri);
            var sanitizedUrls = HtmlParser.SanitizeUrls(urls);
            var (baseUrls, otherUrls) = SeparateUrls(sanitizedUrls, baseUri);

            AddUrlsToOutputModel(baseUrls, otherUrls);


            return (baseUrls, otherUrls);
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
            var pageO = await _cache.GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);
            if (_options.EnableTagSearch || _options.HtmlTagToSearchFor is not null)
                {
                    if (HtmlParser.SearchPageForTagName(pageO.Content, "video"))
                        {
                            OutputControl.Instance!.CapturedUrlWithSearchResults?.Add(url);
                        }
                }

            var urls = HtmlParser.GetHrefLinksFromDocumentSource(pageO.Content);

            // Log.Debug($"Count togo: {this.UrlsInQueue}");


            // Validate urls and separate urls
            var cleanUrls = VerifyAndSeparateUrls(urls, _options.StartingUrl);

            OutputControl.Instance.CapturedExternalLinks.AddArray(cleanUrls.OtherUrls.ToArray());
            OutputControl.Instance.CapturedSeedLinks.AddArray(cleanUrls.BaseUrls.ToArray());

            if (_options.FollowExternalLinks)
                {
                    cleanUrls.BaseUrls.AddRange(cleanUrls.OtherUrls);
                }


            return cleanUrls.BaseUrls;
        }

    #endregion
}