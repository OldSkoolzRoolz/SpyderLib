#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Control;

public class SpyderControl : ISpyderControl
{
    private ICacheControl _cacheControl;
    private readonly ILogger _logger;
    private KC.Apps.SpyderLib.Modules.ISpyderWeb _spyderWeb;





    public SpyderControl(ILoggerFactory                      factory, ICacheControl cache,
        IOptions<KC.Apps.SpyderLib.Properties.SpyderOptions> options)
    {
        _logger = factory.CreateLogger<SpyderControl>();
        _spyderWeb =
            new KC.Apps.SpyderLib.Modules.SpyderWeb(logger: _logger, options: options.Value, cacheControl: CacheCtl);
        Factory = factory;
        CacheCtl = cache;
        CrawlerOptions = options.Value;
    }





    public static ICacheControl CacheCtl { get; private set; }
    public static KC.Apps.SpyderLib.Properties.SpyderOptions CrawlerOptions { get; private set; }
    public static ILoggerFactory Factory { get; private set; }





    /// <summary>
    ///     Instructs Spyder to crawl each link in the input file
    /// </summary>
    /// <returns></returns>
    public Task BeginProcessingInputFileAsync()
    {
        return null;
    }





    /// <summary>
    ///     Set the depth and the starting url and crawl the web
    /// </summary>
    /// <param name="seedUrl"></param>
    public Task BeginSpyder(string seedUrl)
    {
        return null;
    }





    /// Worker
    /// <summary>
    /// </summary>
    public Task ScrapeSingleSiteAsync()
    {
        return null;
    }





    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return null;
    }





    /// <summary>
    ///     Starts Crawler according to options already set during initialization
    /// </summary>
    /// <returns>Task</returns>
    public Task StartCrawlingAsync()
    {
        return null;
    }





    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return null;
    }
}