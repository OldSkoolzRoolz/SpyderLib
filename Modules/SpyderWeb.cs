#region

// ReSharper disable All
using System.Diagnostics;

using HtmlAgilityPack;

using KC.Apps.Control;
using KC.Apps.Interfaces;
using KC.Apps.Logging;
using KC.Apps.Models;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SpyderLib.Modules;

#endregion

namespace KC.Apps.Modules;



/// <summary>
/// </summary>
public class SpyderWeb : ISpyderWeb
{
    private readonly IndexCacheService _cacheControl;
    private readonly ILogger _logger;
    private readonly SpyderOptions _options;
    private readonly IOutputControl _output;
    private readonly SemaphoreSlim _semaphore;
    private readonly IBackgroundTaskQueue _taskQue;
    private ConcurrentScrapedUrlCollection NewlyScrapedUrls = new();
    private ConcurrentScrapedUrlCollection ScrapingTargets = new();





    /// <summary>
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <param name="cacheControl"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SpyderWeb(
        ILogger logger, IOptions<SpyderOptions> options, IndexCacheService cacheControl
    )
        {
            ArgumentNullException.ThrowIfNull(argument: logger);
            ArgumentNullException.ThrowIfNull(argument: options);
            ArgumentNullException.ThrowIfNull(argument: cacheControl);
            //_taskQue = taskQueue;
            _logger = logger;
            _options = options.Value;
            _cacheControl = cacheControl;
            _output = new OutputControl(options: _options);
            _logger.LogDebug(message: "SpyderWeb Initialized");
            _semaphore = new(5, 5);
        }





    /// <summary>
    /// </summary>
    public async Task ProcessInputFileAsync()
        {
            using var fo = new FileOperations();
            //Load links from file
            var links = fo.LoadLinksFromInputFile(filename: _options.InputFileName);

            // Ensure valid url string structure
            var cleanlinks = links.Select(link => link)
                .Where(predicate: SpyderHelpers.IsValidUrl);

            // Create scraping tasks
            var tasks = cleanlinks.Select(selector: GetPageSourceAndParseForLinksAsync);

            // Process all the tasks with throttling
            await ProcessTasksAsync(tasks: tasks);
            Console.WriteLine(value: "Finished processing input file links");
            _output.OnLibraryShutdown();
        }





    /// <summary>
    ///     Search for tag identified in Spyder Options
    /// </summary>
    /// <param name="url"></param>
    async Task ISpyderWeb.ScrapePageForHtmlTagAsync(string url)
        {
            try
            {
                HtmlDocument doc = new();
                var htmlDoc = await _cacheControl.GetAndSetContentFromCacheAsync(address: url).ConfigureAwait(false);
                doc.LoadHtml(html: htmlDoc);
                if (HtmlParser.SearchPageForTagName(htmlDocument: doc, tag: _options.HtmlTagToSearchFor))
                {
                    _output.CapturedVideoLinks.Add(url: url);
                }
            }
            catch (Exception)
            {
                _logger.SpyderWebException($"Unknown error was during crawl of a page {url}");
                // Log and continue Failed tasks won't hang up the flow. Possible retry?            
            }
        }





    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    async Task ISpyderWeb.StartSpyderAsync(string startingLink)
        {
            _options.StartingUrl = startingLink;
            try
            {
                ScrapingTargets.Add(url: startingLink);
                await ExploreWebPagesAsync();
                _logger.GeneralSpyderMessage(message: "Scraping Complete");
            }
            catch (Exception)
            {
                _logger.SpyderWebException(message: "Unhandled exception during scraping of a webpage");
            }
            finally
            {
                _output.OnLibraryShutdown();
            }
        }





    /// <summary>
    ///     Gets links from local cache or the web and populates newlinks
    ///     Links are filtered according to options set in SpyderOptions
    /// </summary>
    /// <param name="link"></param>
    internal async Task GetPageSourceAndParseForLinksAsync(string link)
        {
            HtmlDocument htmlDoc = new();
            try
            {
                var pageSource = await _cacheControl.GetAndSetContentFromCacheAsync(link).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(value: pageSource))
                {
                    htmlDoc.LoadHtml(html: pageSource);
                }

                var links = HtmlParser.GetHrefLinksFromDocument(doc: htmlDoc);
                if (links is { Count: > 0 })
                {
                    //Filter out links according to SpyderOptions
                    var filteredlinks = SpyderHelpers.FilterScrapedCollection(
                        collection: links, spyderOptions: _options);

                    //Add clean links to our global collection for next round          
                    NewlyScrapedUrls.AddRange(filteredlinks);
                    //Save Index
                }
            }
            catch (TaskCanceledException tce)
            {
                _logger.SpyderWebException(message: tce.Message);
            }
            catch (Exception)
            {
                // Log error and Continue to next url
                _logger.SpyderWebException(message: "Unknown error occured during link filtering");
            }
        }





    /// <summary>
    /// </summary>
    Task StartScrapingInputFileAsync()
        {
            var links = SpyderHelpers.LoadLinksFromFile(filename: _options.InputFileName);
            if (links is null)
            {
                _logger.GeneralSpyderMessage(message: "No links found in input file. check your file and try again");
                return Task.CompletedTask;
            }

            //LINQ
            var urls = links.Select(link => link.Key);
            try
            {
                //   var tasks = urls.Select(selector: StartSpyderAsync);
                // Task.WaitAll(tasks.ToArray());
            }
            catch (Exception)
            {
                _logger.SpyderWebException(message: "General exception, crawling aborted.");
            }
            finally
            {
                _output.OnLibraryShutdown();
            }

            return Task.CompletedTask;
        }





    /// <summary>
    /// </summary>
    private async Task ExploreWebPagesAsync()
        {
            var depthLevel = 0;
            while (ScrapingTargets.Any() && depthLevel < _options.ScrapeDepthLevel)
            {
                await ScrapeCurrentDepthLevel();
            }
        }





    /// <summary>
    ///     Generic method for processing tasks with throttling
    /// </summary>
    /// <param name="tasks"></param>
    public async Task ProcessTasksAsync(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
            {
                await _semaphore.WaitAsync();
                await task.ContinueWith(
                    t =>
                    {
                        // Always release the semaphore 
                        // regardless of the task result.
                        _semaphore.Release();
                    });

                await task;
            }
        }





    private async Task ScrapeAndLog(string link)
        {
            ArgumentNullException.ThrowIfNull(argument: link);
            await GetPageSourceAndParseForLinksAsync(link: link);
        }





    private async Task ScrapeCurrentDepthLevel()
        {
            var scrapeTasks =
                ScrapingTargets.Select((link, index) => ScrapeAndLog(link: link.Key));

            await Task.WhenAll(tasks: scrapeTasks);
            _cacheControl.SaveCacheIndex();
            //Link shuffle for next level
            ScrapingTargets.Clear();
            // Add new scraped links to looping collection
            ScrapingTargets.AddRange(itemsToAdd: NewlyScrapedUrls);
            // Add newly scraped links to output collection
            _output.UrlsScrapedThisSession.AddRange(itemsToAdd: NewlyScrapedUrls);
            NewlyScrapedUrls.Clear();
        }





    /// <summary>
    ///     Method gets the page source and parses it for video lnks
    /// </summary>
    /// <param name="url"></param>
    public async Task ScrapePageForVideoLinksAsync(Uri url)
        {
            await _semaphore.WaitAsync();
            try
            {
                //Method gets page from cache or from the web.
                // This should never be null
                var strdoc = await _cacheControl.GetAndSetContentFromCacheAsync(address: url.AbsoluteUri);
                Debug.Assert(!string.IsNullOrEmpty(value: strdoc));
                var doc = new HtmlDocument();
                doc.LoadHtml(html: strdoc);
                var videoLinks = HtmlParser.GetVideoLinksFromDocument(doc: doc);
                _output.CapturedVideoLinks.AddArray(array: videoLinks);
                _output.OnLibraryShutdown();
            }
            finally
            {
                _semaphore.Release();
            }
        }
}