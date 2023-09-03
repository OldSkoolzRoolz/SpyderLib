#region

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using PuppeteerSharp;

#endregion

namespace KC.Apps.SpyderLib.Control;

internal interface IBrowserControl
{
    IBrowser Browser { get; }
    ILoggerFactory MyLoggerFactory { get; set; }


    void Dispose();


    Task<ConcurrentScrapedUrlCollection> ExtractHyperLinksAsync(string url);


    Task<HtmlDocument> GetPageDocumentAsync(string url);
}