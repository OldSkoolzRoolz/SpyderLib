#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using HtmlAgilityPack;

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Modules;

public interface IPageCrawler
{
    #region Public Methods

    Task BeginCrawlingSingleSiteAsync(QueItem queItem);



    #endregion
}




/// <summary>
/// Class represents a page spider. Multiple crawlers can be run as any time up
/// to the limit set in options. Each crawler operates on a single url.
/// </summary>
public class PageCrawler : IPageCrawler
{
    #region Other Fields

    private readonly CancellationToken _crawlerCancellationToken;
    private readonly ICrawlerQue _crawlerQue;
    private QueItem _currentQueItem;
    private readonly IBackgroundDownloadQue _downloadQue;

    private readonly ILogger<PageCrawler> _logger;

    private readonly Stopwatch _sw = new();
    private ICacheIndexService _cache;

    private SpyderOptions _options;

    #endregion

    #region Interface Members

   
  





    public async Task BeginCrawlingSingleSiteAsync(QueItem queItem)
        {
            _currentQueItem = queItem;
                _logger.LogTrace($"Crawler beginning crawl task of {_currentQueItem.Url}");
           await ScrapeAndLog(_currentQueItem.Url);

        }






    #endregion

    #region Public Methods

    public PageCrawler(
        ICrawlerQue            crawlQue,
        IBackgroundDownloadQue downloadQue,
        ILogger<PageCrawler>   createLogger,
        CancellationToken      token,
        SpyderOptions          options)
        {
            _downloadQue = downloadQue;
            _logger = createLogger;
            _crawlerCancellationToken = token;
            _options = options;
            _crawlerQue = crawlQue;
            _cache = ModFactory.Instance.GetCacheIndex();
        }





    public async Task BuildDownloadTaskAsync(CancellationToken token,
                                             string            downloadUrl)
        {

            var dl = new DownloadItem(downloadUrl, "/Data/Spyder/Files");

            await _downloadQue.QueueBackgroundWorkItemAsync(dl).ConfigureAwait(false);

        }





  


    public ConcurrentScrapedUrlCollection NewLinks { get; } = new();

    #endregion

    #region Private Methods






    private async Task ScrapeAndLog(
        string link)
        {
            
            ArgumentNullException.ThrowIfNull(link);

            var pageContent = await _cache.GetAndSetContentFromCacheAsync(link).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(pageContent.Content))
                {
                    var links = ParsePageContentForLinksAsync(pageContent);
                     ClassifyCapturedUrl(links);
                     if (_options.EnableTagSearch)
                         {
                            // await SearchDocForHtmlTagAsync(pageContent.Content, link);
                         }
                }

        }




/// <summary>
/// filters newly captured links according to the options and sends them to the crawler controller
/// </summary>
/// <param name="links"></param>
/// <returns></returns>
    private void ClassifyCapturedUrl(string[] links)
        {
            var cleaned = SpyderHelpers.ClassifyScrapedUrls(links, _options);
            foreach (var key in cleaned.Keys)
                {
                 //   _crawlControl.AddCapturedUrl(key);
                }
        }





    private string[] ParsePageContentForLinksAsync(PageContent pageContent)
        {
            ArgumentNullException.ThrowIfNull(pageContent);

            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent.Content);

            var hyperlinks =HtmlParser.GetHrefLinksFromDocument(doc);

            return hyperlinks;
        }





 
    #endregion
}