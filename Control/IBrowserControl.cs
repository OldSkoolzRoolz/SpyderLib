#region

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using PuppeteerSharp;

using SpyderLib.Models;

#endregion

namespace SpyderLib.Control;

internal interface IBrowserControl
{
    IBrowser Browser { get; }
    ILoggerFactory MyLoggerFactory { get; set; }

    #region Methods

    void Dispose();


    Task<ConcurrentScrapedUrlCollection> ExtractHyperLinksAsync(string url);


    Task<HtmlDocument> GetPageDocumentAsync(string url);

    #endregion
}