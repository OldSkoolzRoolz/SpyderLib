#region

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Modules;

public class SpyderWeb : ServiceBase, ISpyderWeb, IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly IWebCrawlerController _crawlerController;
    private readonly IDownloadControl _downloadControl;
    private readonly ILogger _logger;
    private bool _disposedValue;

    #region Interface Members

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SpyderWeb()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }





    public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }





    public void SearchLocalCacheForTags()
        {
            _ = _downloadControl.SearchLocalCacheForHtmlTag();
        }





    public Task StartScrapingInputFileAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
                {
                    if ((this.Options.CrawlInputFile && string.IsNullOrWhiteSpace(value: this.Options.InputFileName)) ||
                        !File.Exists(path: this.Options.InputFileName))
                        {
                            throw new SpyderOptionsException(message: "Check settings and try again.");
                        }
                }
            catch (Exception e)
                {
                    throw new SpyderOptionsException(message: e.Message);
                }

            var links = SpyderHelpers.LoadLinksFromFile(filename: this.Options.InputFileName);
            if (links is null)
                {
                    _logger.SpyderInfoMessage(message: "No links found in input file. check your file and try again");


                    return Task.CompletedTask;
                }


            var urls = links.Select(link => link.Key);
            try
                {
                    Task.WaitAll(
                        urls.Select(url => StartSpyderAsync(startingLink: url, token: _cancellationToken)).ToArray(),
                        cancellationToken: _cancellationToken);
                }
            catch (SpyderException e)
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
        string startingLink,
        CancellationToken token)
        {
            _logger.SpyderTrace(message: "Crawler loading up starting url");
            try
                {
                    _logger.SpyderDebug($"Engaging crawler for seed url: {startingLink}");
                    await EngagePageCrawlerAsync(token: token).ConfigureAwait(false);

                    _logger.SpyderTrace(message: "Finished crawling tasks.");
                    _logger.SpyderInfoMessage(message: "Scraping Complete");
                }
            catch (SpyderException)
                {
                    _logger.SpyderWebException(message: "Unhandled exception during scraping of a webpage");
                }
        }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Constructs an instance of the <see cref="SpyderWeb" /> class.
    /// </summary>
    /// <param name="downloadControl"></param>
    /// <param name="logger">An instance of a logger configured for the `SpyderWeb` class.</param>
    /// <param name="crawlerController">An instance that provides web crawling functionalities.</param>
    public SpyderWeb(IDownloadControl downloadControl,
        ILogger<SpyderWeb> logger,
        IWebCrawlerController crawlerController)
        {
            StartupComplete = new();
            //_options = spyderOptions.Value;
            _crawlerController = crawlerController;
            _logger = logger;
            _logger.SpyderDebug(message: "SpyderWeb Initialized");
            _ = StartupComplete.TrySetResult(true);
            _downloadControl = downloadControl;
        }





    public static Task DownloadVideoTagsFromUrl(
        Uri url)
        {
            return Task.CompletedTask;
        }





    public static TaskCompletionSource<bool> StartupComplete { get; private set; } = new();

    #endregion

    #region Private Methods

    protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
                {
                    if (disposing)
                        {
                            // TODO: dispose managed state (managed objects)
                        }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    _disposedValue = true;
                }
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
    private async Task EngagePageCrawlerAsync(
        CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _logger.SpyderDebug(message: "Engaging page crawler, starting first level");

            try
                {
                    await _crawlerController.StartCrawlingAsync(token: token).ConfigureAwait(false);
                }
            catch (SpyderException)
                {
                    // This should not execute. Error handling should be internal to the crawler
                    _logger.InternalSpyderError(message: "An Exception slipped past the net. Crawling is aborted");
                }
        }

    #endregion

    //private readonly SpyderOptions _options;
}