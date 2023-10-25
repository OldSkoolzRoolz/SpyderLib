using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HtmlAgilityPack;
using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KC.Apps.SpyderLib.Modules;

public interface IPageCrawler
{
    bool CrawlerActive { get; set; }




    void InitializeCrawler(SpyderOptions options, CancellationToken token, IServiceProvider provider);




    Task BeginCrawlingAsync(CancellationToken token);
}

public class PageCrawler : IPageCrawler
{
    private readonly List<Task> _backgroundTasks;
    private readonly IBackgroundDownloadQue _downloadQue;
    private readonly ILogger<PageCrawler> _logger;
    private readonly ConcurrentScrapedUrlCollection _scrapingTargets = new();
    private readonly Stopwatch _sw = new();
    private CacheIndexService _cache;

    private int _htmlTagHits;
    private int _linksCapturedThisSession;
    private SpyderOptions _options;
    private int _urlsProcessedThisSession;





    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public PageCrawler(IBackgroundDownloadQue downloadQue, ILogger<PageCrawler> logger)
        {
            _downloadQue = downloadQue ?? throw new ArgumentNullException(nameof(downloadQue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundTasks = new List<Task>();
        }





    public ConcurrentScrapedUrlCollection NewLinks { get; } = new();





    [MemberNotNull("_logger", "_startingUrl")]
    public void InitializeCrawler(SpyderOptions options, CancellationToken token, IServiceProvider provider)
        {
            _options = options;
            _cache = ActivatorUtilities.GetServiceOrCreateInstance<CacheIndexService>(provider);
            this.CrawlerActive = true;
            _scrapingTargets.Add(_options.StartingUrl);
        }





    public async Task DownloadVideoTagsFromUrl(string url)
        {
            if (url != null)
                {
                    var content = await _cache.GetAndSetContentFromCacheAsync(url).ConfigureAwait(false);

                    await SearchDocForHtmlTagAsync(content.Content!, url).ConfigureAwait(false);




                }


        }





    public async Task BeginCrawlingAsync(CancellationToken token)
        {
            var depthLevel = 0;
            _scrapingTargets.Add(_options.StartingUrl);
            _sw.Start();


            while (_scrapingTargets.Any() && depthLevel < _options.ScrapeDepthLevel && !token.IsCancellationRequested)
                {
                    _logger.LogTrace($"Crawler beginning loop # {depthLevel}");

                    await ProcessTasksAsync(_scrapingTargets.Select(link => ScrapeAndLog(link.Key)))
                        .ConfigureAwait(false);

                    //Clear targets just scanned
                    _scrapingTargets.Clear();

                    // Add links just captured to be scraped next
                    _scrapingTargets.AddRange(this.NewLinks);

                    // Add newly scraped links to output collection
                    OutputControl.UrlsScrapedThisSession.AddRange(this.NewLinks);

                    // Clear and set up for next level
                    this.NewLinks.Clear();

                    depthLevel++;
                    _cache.SaveCacheIndexPublicWrapper();
                }

            _sw.Stop();
            PrintCrawlStats();
        }





    private void PrintCrawlStats()
        {
            Console.WriteLine($"Links Captured: {_linksCapturedThisSession}");
            Console.WriteLine($"Links Crawled:  {_urlsProcessedThisSession}");
            Console.WriteLine($"Html Tag Search hits  {_htmlTagHits}");
            Console.WriteLine($"Cache Hits this session: {CacheIndexService.CacheHits}");
            Console.WriteLine($"Cache Misses this session: {CacheIndexService.CacheMisses}");
            Console.WriteLine($"Elapsed Time: {_sw.Elapsed.ToString()}");
            Console.WriteLine($"Tasks in Que {_downloadQue.Count}");
        }





    /// <summary>
    ///     Generic method for processing tasks with throttling
    /// </summary>
    /// <param name="tasks"></param>
    public async Task ProcessTasksAsync(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
                {

                    await task.ContinueWith(
                                            _ =>
                                                {
                                                    // Always release the semaphore 
                                                    // regardless of the task result.

                                                }).ConfigureAwait(false);

                    await task.ConfigureAwait(false);
                }
        }





    private async Task ScrapeAndLog(string link)
        {
            _urlsProcessedThisSession++;
            ArgumentNullException.ThrowIfNull(link);

            var pageContent = await _cache.GetAndSetContentFromCacheAsync(link).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(pageContent.Content))
                {
                    this.NewLinks.AddRange(ParsePageContentForLinks(pageContent));
                }
        }





    public bool CrawlerActive { get; set; }





    private ConcurrentScrapedUrlCollection ParsePageContentForLinks(PageContent pageContent)
        {
            ArgumentNullException.ThrowIfNull(pageContent);

            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent.Content);

            var filteredlinks = SpyderHelpers.ClassifyScrapedUrls(
                                                                  HtmlParser.GetHrefLinksFromDocument(doc)!, _options);

            if (_options.EnableTagSearch && pageContent.Content != null)
                {
                    _backgroundTasks.Add(SearchDocForHtmlTagAsync(pageContent.Content, pageContent.Url));
                }

            _linksCapturedThisSession += filteredlinks.Count;
            return filteredlinks;
        }





    /// <summary>
    ///     Search <see cref="HtmlDocument" /> for tag identified in Spyder Options
    /// </summary>
    /// <param name="content"></param>
    /// <param name="url"></param>
    private async Task SearchDocForHtmlTagAsync(string content, string url)
        {
            var doc = HtmlParser.CreateHtmlDocument(content);
            try
                {
                    if (HtmlParser.TryExtractUserTagFromDocument(doc, _options.HtmlTagToSearchFor,
                                                                 out var extractedLinks))
                        {
                            OutputControl.CapturedVideoLinks.Add(url);
                            _htmlTagHits++;

                            if (_options.DownloadTagSource)
                                {
                                    foreach (var link in extractedLinks)
                                        {
                                            await BuildDownloadTaskAsync(CancellationToken.None, link.Key)
                                                .ConfigureAwait(false);
                                        }
                                }
                        }
                }
            catch (Exception)
                {
                    _logger.SpyderWebException($"Eerror parsing page document {url}");

                    // Log and continue Failed tasks won't hang up the flow. Possible retry?            
                }
        }





    public async Task BuildDownloadTaskAsync(CancellationToken token, string downloadUrl)
        {

            var dl = new DownloadItem(downloadUrl, "/Data/Spyder/Files");

            await _downloadQue.QueueBackgroundWorkItemAsync(dl);

        }
}