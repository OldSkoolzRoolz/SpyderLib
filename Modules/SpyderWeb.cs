#region

using HtmlAgilityPack;

using JetBrains.Annotations;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using SpyderLib.Control;
using SpyderLib.Logging;
using SpyderLib.Models;
using SpyderLib.Properties;

#endregion

//TODO: Seperate out sub-classes SRP
//TODO: Implement async cancellation
//TODO: Move output to outputcontroller;

namespace SpyderLib.Modules;

/// <summary>
/// </summary>
public class SpyderWeb : IDisposable, ISpyderWeb
{
    private readonly ICacheControl _cacheControl;
   // private readonly HtmlParser _htmlParser;
    private readonly ILogger _logger;
    private readonly SpyderOptions _options;
    private IOutputControl _output;
    private readonly SemaphoreSlim _semaphore;
 //   private readonly Lazy<SpyderHelpers> _spyderHelpers;





    /// <summary>
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SpyderWeb([NotNull]ILogger logger,[NotNull] SpyderOptions options, [NotNull]ICacheControl cacheControl)
    {
        ArgumentNullException.ThrowIfNull(argument: logger);
        _options = options ??
                   throw new ArgumentNullException(nameof(options), message: "SpyderOptions cannot be null");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), message: "ILogger cannot be null");

        _output = new OutputControl(_options);
        _cacheControl = cacheControl;
        _logger.LogDebug(message: "SpyderWeb Initialized");
     
        _semaphore = new(5, 5);
    }





    #region Methods

    public void Dispose()
    {
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }





    private async Task ExploreWebPagesAsync(ConcurrentScrapedUrlCollection scrapingTargets,
        ConcurrentScrapedUrlCollection                                     newLinks)
    {
        var depthLevel = 0;

        while (scrapingTargets.Any() && depthLevel < _options.ScrapeDepthLevel)
        {
            await ScrapeCurrentDepthLevel(scrapingTargets: scrapingTargets, newlinks: newLinks,
                                          depthLevel: ++depthLevel);
        }
    }





    private void OnCacheShutdown()
    {
        _logger.DebugTestingMessage(message: "Cache shutdown is triggered");
    }





    private void OnControlShutdown()
    {
        _logger.DebugTestingMessage(message: "Spyder Control shutdown triggered");
    }





    /// <summary>
    /// </summary>
    public async Task ProcessInputFileAsync()
    {
        using var fo = new FileOperations(logger: _logger, options: _options);
        //Load links from file
        var links = fo.LoadLinksFromInputFile(filename: _options.InputFileName);

        // Ensure valid url string structure
        var cleanlinks = links.Select(link => link)
                              .Where(predicate: SpyderHelpers.IsValidUrl);

        // Create scraping tasks
        var tasks = cleanlinks.Select(selector: ScrapePageForHtmlTagAsync);

        // Process all the tasks with throttling
        await ProcessTasksAsync(tasks: tasks);

        Console.WriteLine(value: "Finished processing input file links");
        _output.OnLibraryShutdown();
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

            await task.ContinueWith(t =>
                                    {
                                        // Always release the semaphore 
                                        // regardless of the task result.
                                        _semaphore.Release();
                                    });

            await task;
        }
    }





    private async Task ScrapeAndLog(string link, ConcurrentScrapedUrlCollection newlinks, int index)
    {
        await ScrapePageForLinksAsync(link: link);
    }





    private async Task ScrapeCurrentDepthLevel(ConcurrentScrapedUrlCollection scrapingTargets,
        ConcurrentScrapedUrlCollection                                        newlinks, int depthLevel)
    {
        var scrapeTasks =
            scrapingTargets.Select((link, index) => ScrapeAndLog(link: link.Key, newlinks: newlinks, index: index));

        await Task.WhenAll(tasks: scrapeTasks);

        //Link shuffle for next level
        scrapingTargets.Clear();
        // Add new scraped links to looping collection
        scrapingTargets.AddRange(itemsToAdd: newlinks);
        // Add newly scraped links to output collection
        _output.UrlsScrapedThisSession.AddRange(itemsToAdd: newlinks);
        newlinks.Clear();
    }





    /// <summary>
    ///     Search for tag identified in Spyder Options
    /// </summary>
    /// <param name="url"></param>
    public async Task ScrapePageForHtmlTagAsync(string url)
    {
        try
        {
            HtmlDocument doc     = new();
            var          htmlDoc = await _cacheControl.GetWebPageSourceAsync(address: url);
            doc.LoadHtml(html: htmlDoc);

               if(HtmlParser.SearchPageForTagName(htmlDocument: doc, tag: _options.HtmlTagToSearchFor))
               {
                   _output.CapturedVideoLinks.Add(url);

               }
               
        }
        catch (Exception e)
        {
            _logger.SpyderWebException($"Unknown error was during crawl of a page {url}");
            // Log and continue Failed tasks won't hang up the flow. Possible retry?            
        }
    }





    /// <summary>
    ///     Gets links from local cache or the web and populates newlinks
    ///     Links are filtered according to options set in SpyderOptions
    /// </summary>
    /// <param name="link"></param>
    /// <param name="newlinks"></param>
    public async Task<ConcurrentScrapedUrlCollection> ScrapePageForLinksAsync(string link)
    {
        var newlinks = new ConcurrentScrapedUrlCollection();
        HtmlDocument htmlDoc = new();
        try
        {
            var pageSource = await _cacheControl.GetWebPageSourceAsync(address: link);
            if (!string.IsNullOrEmpty(value: pageSource))
            {
                htmlDoc.LoadHtml(html: pageSource);
            }


            var links = HtmlParser.GetHrefLinksFromDocument(doc: htmlDoc);
            //Filter out links according to SpyderOptions
            if (links.Count > 0)
            {
                var filteredlinks = SpyderHelpers.FilterScrapedCollection(collection: links, spyderOptions: _options);
                newlinks.AddArray(array: filteredlinks);
                _output.UrlsScrapedThisSession.AddArray(array: newlinks);
            }
        }
        catch (TaskCanceledException tce)
        {
            _logger.SpyderWebException(message: tce.Message);
        }
        catch (Exception e)
        {
            // Log error and Continue to next url
            _logger.SpyderWebException(message: "Unknown error occured during link filtering");
        }

        return newlinks;
    }





    public async Task ScrapeUrlAsync(Uri url)
    {
        await _semaphore.WaitAsync();
        try
        {
            var strdoc = await _cacheControl.GetWebPageSourceAsync(address: url.AbsoluteUri);
            var doc    = new HtmlDocument();
            doc.LoadHtml(html: strdoc);
            var videoLinks = HtmlParser.GetVideoLinksFromDocument(doc: doc);

            _output.CapturedVideoLinks.AddArray(videoLinks.ToArray());
            _output.OnLibraryShutdown();
        }
        catch
        {
            _logger.SpyderWebException(message: "Error occurec during page scraping");
        }
        finally
        {
            _semaphore.Release();
        }
    }





    /// <summary>
    /// </summary>
    public async Task StartScrapingInputFileAsync()
    {
        var links = SpyderHelpers.LoadLinksFromFile(filename: _options.InputFileName);
        if (links is null)
        {
            _logger.GeneralSpyderMessage(message: "No links found in input file. check your file and try again");
            return;
        }

        foreach (var link in links)
        {
            try
            {
                _options.StartingUrl = link.Key;
                await StartSpyderAsync(startingLink: link.Key);
                _logger.DebugTestingMessage(message: "StartScraper method has returned. successfully");
            }
            catch (Exception e)
            {
                _logger.SpyderWebException(message: "General exception, crawling aborted.");
            }
            finally
            {
                _output.OnLibraryShutdown();
            }
        }
    }





    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    public async Task StartSpyderAsync(string startingLink)
    {
        _options.StartingUrl = startingLink;
        var scrapingTargets = new ConcurrentScrapedUrlCollection();
        var newLinks        = new ConcurrentScrapedUrlCollection();

        try
        {
            scrapingTargets.Add(url: startingLink);
            await ExploreWebPagesAsync(scrapingTargets: scrapingTargets, newLinks: newLinks);

            _logger.GeneralSpyderMessage(message: "Scraping Complete");
        }
        catch (Exception e)
        {
            _logger.SpyderWebException(message: "Unhandled exception during scraping of a webpage");
        }
        finally
        {
            _output.OnLibraryShutdown();
        }
    }

    #endregion
}