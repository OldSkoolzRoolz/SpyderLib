#region

using System.Diagnostics;

using HtmlAgilityPack;

using KC.Apps.Logging;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;

#endregion




namespace KC.Apps.SpyderLib.Modules;




/// <summary>
/// </summary>
public class SpyderWeb : ISpyderWeb
    {
        #region Instance variables

        private readonly List<Task> _backgroundTasks = new();

        private readonly IndexCacheService _cacheControl;
        private int _HtmlTagHits;
        private int _linksCapturedThisSession;
        private readonly ILogger _logger;
        private static SpyderOptions _options;
        private readonly SemaphoreSlim _semaphore;
        private readonly IBackgroundTaskQueue _taskQue;
        private int _urlsProcessedThisSession;
        private readonly ConcurrentScrapedUrlCollection NewlyScrapedUrls = new();
        private readonly ConcurrentScrapedUrlCollection ScrapingTargets = new();

        #endregion





        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SpyderWeb(SpyderOptions options, IndexCacheService cache)
            {
                ArgumentNullException.ThrowIfNull(options);
                _logger = SpyderControlService.LoggerFactory.CreateLogger<SpyderWeb>();
                _options = options;
                _logger.LogDebug("SpyderWeb Initialized");
                _semaphore = new SemaphoreSlim(5, 5);
                _cacheControl = cache;
            }





        #region Methods

        /// <summary>
        ///     Generic method for processing tasks with throttling
        /// </summary>
        /// <param name="tasks"></param>
        public async Task ProcessTasksAsync(IEnumerable<Task> tasks)
            {
                foreach (var task in tasks)
                    {
                        await _semaphore.WaitAsync().ConfigureAwait(false);
                        await task.ContinueWith(
                            t =>
                                {
                                    // Always release the semaphore 
                                    // regardless of the task result.
                                    _semaphore.Release();
                                }).ConfigureAwait(false);

                        await task.ConfigureAwait(false);
                    }
            }





        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SpyderOptionsException"></exception>
        public Task StartScrapingInputFileAsync()
            {
                if ((_options.CrawlInputFile && string.IsNullOrWhiteSpace(_options.InputFileName)) ||
                    !File.Exists(_options.InputFileName))
                    {
                        throw new SpyderOptionsException("Check settings and try again.");
                    }

                var links = SpyderHelpers.LoadLinksFromFile(_options.InputFileName);
                if (links is null)
                    {
                        _logger.GeneralSpyderMessage("No links found in input file. check your file and try again");
                        return Task.CompletedTask;
                    }


                var urls = links.Select(link => link.Key);
                try
                    {
                        var tasks = urls.Select(StartSpyderAsync);
                        Task.WaitAll(tasks.ToArray());
                    }
                catch (Exception e)
                    {
                        _logger.SpyderWebException("General exception, crawling aborted.");
                    }

                return Task.CompletedTask;
            }





        /// <summary>
        ///     Main spyder method starts crawling the given link according to options set
        /// </summary>
        /// <param name="startingLink"></param>
        public async Task StartSpyderAsync(string startingLink)
            {
                _logger.LogTrace("Crawler loading up starting url");
                try
                    {
                        var sw = new Stopwatch();
                        sw.Start();

                        // Add initial url (level 0)
                        ScrapingTargets.Add(startingLink);
                        _logger.LogDebug("Engaging crawler for seed url: {0}", startingLink);
                        await EngagePageCrawlerAsync(CancellationToken.None).ConfigureAwait(false);
                        sw.Stop();
                        _logger.LogTrace("Finished crawling tasks.");
                        _logger.GeneralSpyderMessage("Scraping Complete");
                        Console.WriteLine($"Links Captured: {_linksCapturedThisSession}");
                        Console.WriteLine($"Links Crawled:  {_urlsProcessedThisSession}");
                        Console.WriteLine($"Html Tag Search hits  {_HtmlTagHits}");
                        Console.WriteLine($"Cache Hits this session: {IndexCacheService.CacheHits}");
                        Console.WriteLine($"Cache Misses this session: {IndexCacheService.CacheMisses}");
                        Console.WriteLine($"Elapsed Time: {sw.Elapsed.ToString()}");
                    }
                catch (Exception)
                    {
                        _logger.SpyderWebException("Unhandled exception during scraping of a webpage");
                    }
            }

        #endregion




        #region Methods

        private ConcurrentScrapedUrlCollection ParsePageContentForLinks(PageContent pageContent)
            {
                ArgumentNullException.ThrowIfNull(pageContent);

                var doc = new HtmlDocument();
                doc.LoadHtml(pageContent.Content);
                if (_options.EnableTagSearch && !string.IsNullOrWhiteSpace(_options.HtmlTagToSearchFor))
                    {
                        _backgroundTasks.Add(SearchDocForHtmlTagAsync(doc, pageContent.Url));
                    }

                var filteredlinks = SpyderHelpers.ClassifyScrapedUrls(
                    HtmlParser.GetHrefLinksFromDocument(doc)!, _options);

                _linksCapturedThisSession += filteredlinks.Count;
                return filteredlinks;
            }





        /// <summary>
        ///     Control loop for the depth of site scrapes
        /// </summary>
        /// <param name="token">CancellationToken to abort operations</param>
        private async Task EngagePageCrawlerAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                _logger.LogDebug("Engaging page crawler, starting first level");
                var depthLevel = 0;
                while (ScrapingTargets.Any() && depthLevel < _options.ScrapeDepthLevel)
                    {
                        _logger.LogDebug(
                            "Now crawling level {0} count of {1} links", depthLevel, ScrapingTargets.Count);

                        await ScrapeCurrentDepthLevel(token).ConfigureAwait(false);

                        _logger.LogTrace("Saving cache index");
                        _cacheControl.SaveCacheIndex();
                        depthLevel++;
                        token.ThrowIfCancellationRequested();
                    }

                _logger.LogTrace("Crawler shutting down");
            }





        private async Task ScrapeAndLog(string link)
            {
                _urlsProcessedThisSession++;
                ArgumentNullException.ThrowIfNull(link);

                var pageContent = await _cacheControl.GetAndSetContentFromCacheAsync(link).ConfigureAwait(false);

                NewlyScrapedUrls.AddRange(ParsePageContentForLinks(pageContent));
            }





        /// <summary>
        ///     creates a list of task to perform for each target in the ScrapingTargets collection.
        /// </summary>
        private async Task ScrapeCurrentDepthLevel(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                await ProcessTasksAsync(ScrapingTargets.Select(link => ScrapeAndLog(link.Key))).ConfigureAwait(false);

                //Clear targets just scanned
                ScrapingTargets.Clear();

                // Add links just captured to be scraped next
                ScrapingTargets.AddRange(NewlyScrapedUrls);

                // Add newly scraped links to output collection
                OutputControl.UrlsScrapedThisSession.AddRange(NewlyScrapedUrls);
                NewlyScrapedUrls.Clear();
            }





        /// <summary>
        ///     Search <see cref="HtmlDocument" /> for tag identified in Spyder Options
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="url"></param>
        private Task SearchDocForHtmlTagAsync(HtmlDocument doc, string url)
            {
                try
                    {
                        if (HtmlParser.SearchPageForTagName(doc, _options.HtmlTagToSearchFor))
                            {
                                OutputControl.CapturedVideoLinks.Add(url);
                                _HtmlTagHits++;
                            }
                    }
                catch (Exception)
                    {
                        _logger.SpyderWebException($"Eerror parsing page document {url}");

                        // Log and continue Failed tasks won't hang up the flow. Possible retry?            
                    }

                return Task.CompletedTask;
            }

        #endregion
    }