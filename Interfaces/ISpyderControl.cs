#region

using Microsoft.Extensions.Hosting;

#endregion

namespace KC.Apps.SpyderLib.Interfaces;

public interface ISpyderControl : IHostedService
{
    #region Interface Members

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    Task IHostedService.StartAsync(
        CancellationToken cancellationToken)
        {
            return StartAsync(cancellationToken);
        }





    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    Task IHostedService.StopAsync(
        CancellationToken cancellationToken)
        {
            return StopAsync(cancellationToken);
        }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Instructs Spyder to crawl each link in the input file
    /// </summary>
    /// <returns></returns>
    Task BeginProcessingInputFileAsync();





    /// <summary>
    ///     Set the depth and the starting url and crawl the web
    /// </summary>
    /// <param name="seedUrl"></param>
    Task BeginSpyder(
        string seedUrl);





    Task ScrapeSingleSiteAsync();





    /// <summary>
    ///     Starts Crawler according to options already set during initialization
    /// </summary>
    /// <returns>Task</returns>
    Task StartCrawlingAsync();

    #endregion
}