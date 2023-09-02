#region

using System.Net.WebSockets;

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using PuppeteerSharp;

using SpyderLib.Models;

#endregion

namespace SpyderLib.Control;

internal class BrowserControl : IAsyncDisposable, IDisposable
{
    private const string USER_AGENT =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";

    private readonly Lazy<IBrowser> _browser;
    private readonly ILogger _logger;





    internal BrowserControl()
    {
        // _myLoggerFactory = SpyderControl.Factory; 
        _logger = SpyderControl.Factory.CreateLogger(categoryName: "BrowserControl");
        _browser = new(() => OpenBrowserAsync().Result);
    }





    internal IBrowser Browser => _browser.Value;
    internal ILoggerFactory? LoggerFactory { get; }

    #region Methods

    private async Task<IPage> CreatePageAsync()
    {
        var page = await this.Browser.NewPageAsync().ConfigureAwait(false);
        page.DefaultTimeout = 120_000;
        await page.SetUserAgentAsync(userAgent: USER_AGENT).ConfigureAwait(false);
        await page.SetJavaScriptEnabledAsync(true).ConfigureAwait(false);

        return page;
    }





    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }





    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Browser?.Dispose();
            if (this.LoggerFactory is IDisposable disposableFactory)
            {
                disposableFactory.Dispose();
            }
        }
    }





    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(false);
        GC.SuppressFinalize(this);
    }





    /// <summary>
    ///     Contains the high-cost dispose logic
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _browser.Value.DisposeAsync().ConfigureAwait(false);
    }





    public async Task<ConcurrentScrapedUrlCollection> ExtractHyperLinksAsync2(string url)
    {
        var scrapedUrls = new ConcurrentScrapedUrlCollection();


        using (var page = await CreatePageAsync().ConfigureAwait(false))
        {
            try
            {
                // Navigate to the desired address
                await page.GoToAsync(url: url, waitUntil: WaitUntilNavigation.Networkidle2).ConfigureAwait(false);

                // Extract all hyperlinks from the page
                var jsSelectAllAnchors = @"Array.from(document.querySelectorAll('a')).map(a => a.href);";
                var urls = await page.EvaluateExpressionAsync<string[]>(script: jsSelectAllAnchors)
                                     .ConfigureAwait(false);
                if (urls is not null)
                {
                    scrapedUrls.AddArray(array: urls);
                }

                await page.CloseAsync().ConfigureAwait(false);
            }
            catch (WebSocketException wse)
            {
                _logger.LogError(102, message: wse.Message, wse);
            }
            catch (Exception ex)
            {
                // If an error occurs, log it and continue execution
                _logger.LogError(104, message: ex.Message, ex);
            }

            return scrapedUrls;
        }
    }





    private LaunchOptions GetLaunchOptions()
    {
        return new()
               {
                   Headless = true,
                   IgnoreHTTPSErrors = true,
                   LogProcess = false,
                   DefaultViewport = null,
                   UserDataDir = "/Data/Chrome/userdata",
                   ExecutablePath = "/Data/Chrome/Linux-1069273/chrome-linux/chrome",
                   Args = new[]
                          {
                              "--no-sandbox", "--no-zygote", "--disable-setupid-sandbox"
                          },
                   EnqueueTransportMessages = false
               };
    }





    public async Task<HtmlDocument> GetPageDocumentAsync(string url)
    {
        // Creating a new HtmlDocument and ConcurrentScrapedUrlCollection instances
        var htmlDocument = new HtmlDocument();

        using (var page = await CreatePageAsync().ConfigureAwait(false))
        {
            try
            {
                //var response = await page.GoToAsync(url: url, waitUntil: WaitUntilNavigation.DOMContentLoaded);
                var pageContent = await page.GetContentAsync().ConfigureAwait(false);
                htmlDocument.LoadHtml(html: pageContent);
                await page.CloseAsync().ConfigureAwait(false);
            }
            catch (WebSocketException wse)
            {
                _logger.LogError(9888, exception: wse, message: "Web Socket Exception in Puppeteer");
            }
            catch (Exception ex)
            {
                // If an error occurs, log it and continue execution
                _logger.LogError(new(600), exception: ex, message: "General Exception in GetDocument");
            }
        }

        return htmlDocument;
    }





    private async Task<IBrowser> OpenBrowserAsync()
    {
        return await Puppeteer.LaunchAsync(GetLaunchOptions()).ConfigureAwait(false);
    }

    #endregion
}