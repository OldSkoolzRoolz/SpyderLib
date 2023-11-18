#region

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;

using LoggingMessages = KC.Apps.SpyderLib.Logging.LoggingMessages;

#endregion

namespace KC.Apps.SpyderLib.Modules;

public class SpyderWeb : ServiceBase, ISpyderWeb
{
    private readonly CancellationToken _cancellationToken = new();
    private readonly IWebCrawlerController _crawlerController;
    private readonly ILogger _logger;

    #region Private Methods

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
    private async Task EngagePageCrawlerAsync(
        CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _logger.LogDebug("Engaging page crawler, starting first level");

            try
                {
                    await _crawlerController.StartCrawlingAsync(token).ConfigureAwait(false);
                }
            catch (Exception)
                {
                    // This should not execute. Error handling should be internal to the crawler
                    _logger.LogError("A critical error was not handled properly. Details in next log entry.");
                }
        }

    #endregion

    #region Interface Members

    public Task StartScrapingInputFileAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if ((this.Options.CrawlInputFile && string.IsNullOrWhiteSpace(this.Options.InputFileName)) ||
                !File.Exists(this.Options.InputFileName))
                throw new SpyderOptionsException("Check settings and try again.");

            var links = SpyderHelpers.LoadLinksFromFile(this.Options.InputFileName);
            if (links is null)
                {
                    LoggingMessages.GeneralSpyderMessage(_logger,
                        "No links found in input file. check your file and try again");


                    return Task.CompletedTask;
                }


            var urls = links.Select(link => link.Key);
            try
                {
                    Task.WaitAll(urls.Select(url => StartSpyderAsync(url, _cancellationToken)).ToArray(),
                        _cancellationToken);
                }
            catch (Exception e)
                {
                    LoggingMessages.SpyderWebException(_logger, $"General exception, crawling aborted. {e.Message}");
                }


            return Task.CompletedTask;
        }





    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    /// <param name="token"></param>
    public async Task StartSpyderAsync(
        string startingLink,
        CancellationToken token)
        {
            _logger.LogTrace("Crawler loading up starting url");
            try
                {
                    _logger.LogDebug("Engaging crawler for seed url: {0}", startingLink);
                    await EngagePageCrawlerAsync(token).ConfigureAwait(false);

                    _logger.LogTrace("Finished crawling tasks.");
                    LoggingMessages.GeneralSpyderMessage(_logger, "Scraping Complete");
                }
            catch (Exception)
                {
                    LoggingMessages.SpyderWebException(_logger, "Unhandled exception during scraping of a webpage");
                }
        }





    public Task DownloadVideoTagsFromUrl(
        string url)
        {
            return Task.CompletedTask;
        }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Constructs an instance of the <see cref="SpyderWeb" /> class.
    /// </summary>
    /// <param name="logger">An instance of a logger configured for the `SpyderWeb` class.</param>
    /// <param name="crawlerController">An instance that provides web crawling functionalities.</param>
    public SpyderWeb(
        ILogger<SpyderWeb> logger,
        IWebCrawlerController crawlerController)
        {
            StartupComplete = new TaskCompletionSource<bool>();
            //_options = spyderOptions.Value;
            _crawlerController = crawlerController;
            _logger = logger;
            _logger.LogDebug("SpyderWeb Initialized");
            StartupComplete.TrySetResult(true);
        }





    public static TaskCompletionSource<bool> StartupComplete { get; private set; } = new();

    #endregion

    //private readonly SpyderOptions _options;
}