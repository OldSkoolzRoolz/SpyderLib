#region

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.SpyderLib.Modules;


public class SpyderWeb : ServiceBase, ISpyderWeb
{
    #region Other Fields

    private readonly IWebCrawlerController _crawlerController;

    private readonly ILogger _logger;
    private readonly SpyderOptions _options;
    private readonly CancellationToken _cancellationToken = new();

    #endregion

    #region Interface Members

   
    public Task StartScrapingInputFileAsync(CancellationToken token)
        {

            token.ThrowIfCancellationRequested();
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
                            tasks.Add(StartSpyderAsync(url, _cancellationToken));
                        }

                    Task.WaitAll(tasks.ToArray(), _cancellationToken);
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
    public async Task StartSpyderAsync(
        string            startingLink,
        CancellationToken token)
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
    /// <param name="spyderOptions">An instance that provides spyder options configurations.</param>
    /// <param name="logger">An instance of a logger configured for the `SpyderWeb` class.</param>
    /// <param name="crawlerController">An instance that provides web crawling functionalities.</param>
    public SpyderWeb(
        IOptions<SpyderOptions> spyderOptions,
        ILogger<SpyderWeb>      logger,
        IWebCrawlerController   crawlerController)

        {
            StartupComplete = new TaskCompletionSource<bool>();
            _options = spyderOptions.Value;
            _crawlerController = crawlerController;
            _logger = logger;
            _logger.LogDebug("SpyderWeb Initialized");
            StartupComplete.TrySetResult(true);
        }





    public static TaskCompletionSource<bool> StartupComplete { get; set; } = new();

    #endregion

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
                    await _crawlerController.StartCrawlingAsync(_options.LinkDepthLimit,token).ConfigureAwait(false);

                }
            catch (Exception )
                {
                    // This should not execute. Error handling should be internal to the crawler
                    _logger.LogError("A critical error was not handled properly. Details in next log entry.");

                }

        }

    #endregion
}