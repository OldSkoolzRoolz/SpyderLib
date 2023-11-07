#region

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.SpyderLib.Control;

public class WebCrawlerController : IWebCrawlerController
{
    #region Other Fields

    private readonly ICacheIndexService _cache;
    private readonly SpyderOptions _options;
    private readonly IOutputControl _output;
    private readonly ConcurrentDictionary<string, int> _urlsToCrawl;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _crawlerCancellationToken;
    private ConcurrentDictionary<string, bool> _visitedUrls = new ConcurrentDictionary<string, bool>();
    private readonly ILogger<WebCrawlerController> _logger;
    private Stopwatch _crawlTimer;

    #endregion

    #region Interface Members

    /// <summary>
    ///     Starts the web crawling process asynchronously.
    /// </summary>
    /// <param name="maxDepth">The maximum depth to crawl in the web hierarchy.</param>
    /// <param name="token"></param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    /// <remarks>
    ///     The method initializes a CancellationTokenSource that cancels itself after 5 minutes to prevent long running
    ///     crawling operations.
    ///     It also sets up the first level of crawling using the SetupFirstLevel method and then initiates multiple web
    ///     crawling tasks according to the limit provided in _options.ConcurrentCrawlerLimit.
    ///     These tasks are then run asynchronously and in parallel using the Task.WhenAll method.
    ///     If any of these tasks take longer than 5 minutes to execute, a OperationCanceledException will be thrown and the
    ///     operation will be cancelled.
    /// </remarks>
    public async Task StartCrawlingAsync(int maxDepth, CancellationToken token)
        {
            _crawlTimer = new Stopwatch();
            _crawlTimer.Start();
            token.ThrowIfCancellationRequested();
            try
                {
                    var urlsToVisit = new ConcurrentQueue<(string, int)>();
                    urlsToVisit.Enqueue((_options.StartingUrl, 1));


                    await StartWorkerAsync(urlsToVisit, maxDepth,token ).ConfigureAwait(false);
                    /*
                    await Task.WhenAll(Enumerable.Range(0, _options.ConcurrentCrawlingTasks)
                                                 .Select(async _ =>
                                                             await StartWorker(urlsToVisit, maxDepth, token)
                                                                 .ConfigureAwait(false))).ConfigureAwait(false);
*/
                    _logger.SpyderWebException("Tasks have finished");
                    _crawlTimer.Stop();
                    this.CrawlerTasksFinished?.Invoke(this, new CrawlerFinishedEventArgs());
                    PrintStats();
                }
            catch (Exception e)
                {
                    _logger.SpyderWebException(e.Message);
                }

        }





    private async Task StartWorkerAsync(
        ConcurrentQueue<(string, int)> urlsToVisit,
        int                            maxDepth,
        CancellationToken              token)
        {
            while (urlsToVisit.Any())
                {
                    token.ThrowIfCancellationRequested();
                    
                    
                    urlsToVisit.TryDequeue(out var item);
                    var (url, depth) = item;

                    // if crawling this url will exceed the depth limit skip it
                    if ((depth + 1) == maxDepth)
                        {
                            continue;
                        }

                    if (_visitedUrls.TryAdd(url, true))
                        {
                            try
                                {
                                    var newurls = await ProcessUrlAsync(url, token)
                                        .ConfigureAwait(false);
                               
                                    foreach (var uriz in newurls)
                                        {
                                            urlsToVisit.Enqueue((uriz, depth + 1));
                                        }
                                }
                            catch (Exception e)
                                {
#if DEBUG
                                    Log.Error(e.Message);
#endif
                                }
                        }


                }


        }





    public event EventHandler<CrawlerFinishedEventArgs> CrawlerTasksFinished;

    #endregion

    #region Public Methods

    /// <summary>
    ///     Constructor for WebCrawlerController.
    /// </summary>
    /// <param name="options">SpyderOptions options object.</param>
    /// <param name="cache">ICacheIndexService cache object.</param>
    public WebCrawlerController(
        ILogger<WebCrawlerController> logger,
        IOptions<SpyderOptions>       options,
        ICacheIndexService            cache
    )
        {
            _output = OutputControl.Instance;

            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(cache);
            _logger = logger;
            _options = options.Value;
            _cache = cache;
            _urlsToCrawl = new ConcurrentDictionary<string, int>();

            _crawlerCancellationToken = _cancellationTokenSource.Token;

            //Listen for host shutdown message to cleanup
            SpyderControlService.LibraryHostShuttingDown += OnStopping;
        }





    void IWebCrawlerController.CancelCrawlingTasks()
        {
            Log.Warning("A request to cancel all Spyder tasks has been initiated");

            // Cancel any active operations
            _cancellationTokenSource.Cancel();

            _urlsToCrawl.Clear();
        }





    private void OnStopping(object sender, EventArgs e)
        {

            // Log a message notifying that the application domain is being unloaded
            Log.Information("Application domain is being unloaded, stopping all web crawling tasks...");

            // Check if there are tasks still running and take appropriate stop action
            if (!_urlsToCrawl.IsEmpty)
                {
                    _urlsToCrawl.Clear();
                    Log.Information("All crawling tasks have been terminated.");
                }

            try
                {


                    //Save dictionary output 
                    _output.OnLibraryShutdown();
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
            Log.Information("WebCrawlerController has been successfully unloaded.");


        }





    public int UrlsInQueue => _urlsToCrawl.Count;

    #endregion





    private void PrintStats()
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine("**             Spyder Cache Operations              **");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-8}", "Urls Captured", _output.UrlsScrapedThisSession.Count,
                              "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Failed Urls", _output.FailedCrawlerUrls.Count, "**");
            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-9}", "Cache Hits", CacheIndexService.CacheHits, "**");

            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-10}", "Cache Misses", CacheIndexService.CacheMisses, "**");

            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-10}", "Elapsed Time", _crawlTimer.Elapsed.ToString(), "**");


            Console.WriteLine("**                                                  **");
            Console.WriteLine("******************************************************");
        }





    #region Private Methods

    /// <summary>
    ///     Verifies given list of URLs and separates them into base URLs and other URLs.
    ///     Base URLs are those that are based off of a specified starting URL.
    /// </summary>
    /// <param name="urls">IEnumerable collection of URLs to be verified and separated.</param>
    /// <param name="optionsStartingUrl">The starting URL which acts as a base for other URLs.</param>
    /// <returns>
    ///     Returns a tuple where `Item1` is a list of base URLs and `Item2` is a list of other URLs.
    /// </returns>
    /// <remarks>
    ///     If a URL is not valid or cannot be parsed, it will be printed to the console as "Invalid URL: {url}"
    /// </remarks>
    private (List<string> BaseUrls, List<string> OtherUrls) VerifyAndSeparateUrls(
        IEnumerable<string> urls,
        string              optionsStartingUrl)
        {

            var baseUrls = new List<string>();
            var otherUrls = new List<string>();

            try
                {
                    urls = RemoveDuplicatedUrls(urls);
                }
            catch (Exception e)
                {
                    _logger.SpyderWebException(e.Message);


                    throw;
                }

            Uri baseUri;
            if (!Uri.TryCreate(optionsStartingUrl, UriKind.Absolute, out baseUri))
                {
                    _logger.SpyderWebException($"Invalid base URL: {optionsStartingUrl}");


                    return (baseUrls, otherUrls);
                }


            foreach (var url in urls)
                {
                    Uri uri;

                    // Check if URL is valid and try to create a new Uri
                    if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                        {
                            var strippedUrl = uri.GetLeftPart(UriPartial.Path);

                            if (baseUri.IsBaseOf(new Uri(strippedUrl)))
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
                            _logger.LogInformation($"Skipping Invalid URL: {url}");
                        }
                }

            _output.CapturedExternalLinks.AddArray(otherUrls.ToArray());
            _output.CapturedSeedLinks.AddArray(baseUrls.ToArray());
_output.UrlsScrapedThisSession.AddArray(otherUrls.ToArray());
_output.UrlsScrapedThisSession.AddArray(baseUrls.ToArray());

            return (baseUrls, otherUrls);
        }





    /// <summary>
    ///     removes duplicated urls and adds captured to output collection
    /// </summary>
    /// <param name="urls"></param>
    /// <returns>distinct url list not visited this session</returns>
    [return: NotNull]
    private IEnumerable<string> RemoveDuplicatedUrls(IEnumerable<string> urls)
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(urls));

            //Make sure we don't have any duplicates in our new urls
            if (!_output.UrlsScrapedThisSession.IsEmpty)
                {
                    var enumerable = urls as string[] ?? urls.ToArray();
                    _output.UrlsScrapedThisSession.AddArray(enumerable);
                    var distinctUrls = enumerable.Except(_output.UrlsScrapedThisSession.Keys);

                    //We can add original to our scraped collection and let the dictionary filter dupes


                    return distinctUrls;
                }


            return urls;
        }





    // ReSharper disable once UnusedMember.Local
    private async Task WebCrawlerTaskAsync(int maxDepth, CancellationToken ct)
        {
            var currentDepth = 1;
            var nextLevelUrls = new Dictionary<string, int>();
            while (!_urlsToCrawl.IsEmpty && !ct.IsCancellationRequested)
                {
                    foreach (var kvp in _urlsToCrawl)
                        {
                            var url = kvp.Key;
                            var depth = kvp.Value;

                            if (depth <= maxDepth)
                                {
                                    try
                                        {
                                            if (_urlsToCrawl.TryRemove(url, out _))
                                                {
                                                    var newUrls = await ProcessUrlAsync(url, ct);
                                                    foreach (var newUrl in newUrls)
                                                        {
                                                            nextLevelUrls.TryAdd(newUrl, currentDepth + 1);
                                                        }
                                                }
                                        }
                                    catch (Exception e)
                                        {
                                            Log.Error(e.Message);


                                        }
                                }
                            else
                                {
                                    // remove URLs that exceed the maximum depth level
                                    _urlsToCrawl.TryRemove(url, out var depthval);
                                    Log.Trace($"depth: {depthval}");
                                }
                        }



                }

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
                            _output!.CapturedUrlWithSearchResults?.Add(url);
                        }
                }

            var urls = HtmlParser.GetHrefLinksFromDocument(HtmlParser.CreateHtmlDocument(pageO.Content));

            // Log.Debug($"Count togo: {this.UrlsInQueue}");


            // Validate urls and separate urls
            var cleanUrls = VerifyAndSeparateUrls(urls, _options.StartingUrl);

            if (_options.FollowExternalLinks)
                {
                    cleanUrls.BaseUrls.AddRange(cleanUrls.OtherUrls);
                }


            return cleanUrls.BaseUrls;
        }

    #endregion
}