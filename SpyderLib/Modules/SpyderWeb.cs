#region

using KC.Apps.Logging;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Services;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Modules;

/// <summary>
/// </summary>
public class SpyderWeb : ServiceBase, ISpyderWeb
{
    private readonly ILogger _logger;


    private readonly IServiceProvider _provider;
    private readonly IBackgroundDownloadQue _taskQue;





    /// <summary>
    /// </summary>
    /// <param name="taskQue"></param>
    /// <param name="factory"></param>
    /// <param name="provider"></param>
    public SpyderWeb(IBackgroundDownloadQue taskQue, ILoggerFactory factory, IServiceProvider provider)
        {
            _provider = provider;
            _taskQue = taskQue;
            _logger = factory.CreateLogger(nameof(SpyderWeb));
            _logger.LogDebug("SpyderWeb Initialized");

        }





    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="SpyderOptionsException"></exception>
    public Task StartScrapingInputFileAsync()
        {
            if ((this.Options.CrawlInputFile && string.IsNullOrWhiteSpace(this.Options.InputFileName)) ||
                !File.Exists(this.Options.InputFileName))
                {
                    throw new SpyderOptionsException("Check settings and try again.");
                }

            var links = SpyderHelpers.LoadLinksFromFile(this.Options.InputFileName);
            if (links is null)
                {
                    _logger.GeneralSpyderMessage("No links found in input file. check your file and try again");
                    return Task.CompletedTask;
                }


            var urls = links.Select(link => link.Key);
            try
                {
                    List<Task> tasks = new();
                    foreach (var url in urls)
                        {
                            tasks.Add(StartSpyderAsync(url, CancellationToken.None));
                        }

                    Task.WaitAll(tasks.ToArray());
                }
            catch (Exception e)
                {
                    _logger.SpyderWebException($"General exception, crawling aborted. {e.Message}");
                }

            return Task.CompletedTask;
        }





    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    /// <param name="token"></param>
    public async Task StartSpyderAsync(string startingLink, CancellationToken token)
        {
            _logger.LogTrace("Crawler loading up starting url");
            try
                {
                    _logger.LogDebug("Engaging crawler for seed url: {0}", startingLink);
                    await EngagePageCrawlerAsync(token).ConfigureAwait(false);

                    _logger.LogTrace("Finished crawling tasks.");
                    _logger.GeneralSpyderMessage("Scraping Complete");
                }
            catch (Exception)
                {
                    _logger.SpyderWebException("Unhandled exception during scraping of a webpage");
                }


        }





    public async Task DownloadVideoTagsFromUrl(string url)
        {
        }





    //#####################################





    /// <summary>
    ///     Set up and configure the Page Crawler and start it
    ///     NOTE: This method should not catch any exception nor be relied upon
    ///     for any error handling. Error handling should be done internally of
    ///     the crawler itself. This allows for less progress loss and easier recovery on error
    ///     Loss on error should be confined to the parsing and/or crawling of a single url.
    /// </summary>
    /// <param name="token">CancellationToken to abort operations</param>
    /// <returns>Task</returns>
    private async Task EngagePageCrawlerAsync(CancellationToken token)
        {
            var _pageCrawler = new PageCrawler(_taskQue, this.LoggerFactory.CreateLogger<PageCrawler>());
            _pageCrawler.InitializeCrawler(this.Options, token, _provider);

            _logger.LogDebug("Engaging page crawler, starting first level");

            try
                {
                    await _pageCrawler.BeginCrawlingAsync(token).ConfigureAwait(false);
                }
            catch (Exception e)
                {
                    // This should not execute. Error handling should be internal to the crawler
                    _logger.LogError("A critical error was not handled properly. Details in next log entry.");
                    Log.AndContinue(e);

                }
            finally
                {
                    _pageCrawler = null;
                }

            _logger.LogTrace("Crawler shutting down");


        }
}