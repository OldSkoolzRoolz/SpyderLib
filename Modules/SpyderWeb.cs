using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Modules;

[Obsolete(message: "This module will be removed or refactored. Unecessary code.")]
internal class SpyderWeb : ISpyderWeb
{
    //private readonly CancellationToken _cancellationToken;
    private readonly IWebCrawlerController _crawlerController;
    private bool _disposedValue;
    private readonly ILogger _logger;






    /// <summary>
    ///     Constructs an instance of the <see cref="SpyderWeb" /> class.
    /// </summary>
    /// <param name="logger">An instance of a logger configured for the `SpyderWeb` class.</param>
    /// <param name="crawlerController">An instance that provides web crawling functionalities.</param>
    public SpyderWeb(
        ILogger<SpyderWeb> logger,
        IWebCrawlerController crawlerController)
        {
            StartupComplete = new();
            //_options = spyderOptions.Value;
            _crawlerController = crawlerController;
            _logger = logger;
            _logger.SpyderDebug(message: "SpyderWeb Initialized");
            _ = StartupComplete.TrySetResult(true);
        }






    #region Properteez

    public static TaskCompletionSource<bool> StartupComplete { get; private set; } = new();

    #endregion






    #region Public Methods

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
        }






    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    /// <param name="token"></param>
    public async Task StartSpyderAsync(string startingLink, CancellationToken token)
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