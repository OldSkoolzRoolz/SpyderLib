#region

using Microsoft.Extensions.Hosting;

#endregion

namespace KC.Apps.SpyderLib;

public interface ISpyderControl : IHostedService
{
    /// <summary>
    ///     Instructs Spyder to crawl each link in the input file
    /// </summary>
    /// <returns></returns>
    Task BeginProcessingInputFileAsync();





    /// <summary>
    ///     Set the depth and the starting url and crawl the web
    /// </summary>
    /// <param name="seedUrl"></param>
    Task BeginSpyder(string seedUrl);





    /// Worker
    /// <summary>
    /// </summary>
    Task ScrapeSingleSiteAsync();





    /// <summary>
    ///     Starts Crawler according to options already set during initialization
    /// </summary>
    /// <returns>Task</returns>
    Task StartCrawlingAsync();
}