#region

using HtmlAgilityPack;

using KC.Apps.Models;

using Microsoft.Extensions.Logging;

using PuppeteerSharp;

#endregion

namespace KC.Apps.Interfaces;

internal interface IBrowserControl
{
    IBrowser Browser { get; }
    ILoggerFactory MyLoggerFactory { get; set; }


    Task<ConcurrentScrapedUrlCollection> ExtractHyperLinksAsync(string url);


    Task<HtmlDocument> GetPageDocumentAsync(string url);
}