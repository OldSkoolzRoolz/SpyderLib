#region

using Microsoft.Extensions.Logging;

#endregion

namespace SpyderLib.Properties;

/// <summary>
/// </summary>
public interface ISpyderOptions
{
    /// <summary>
    /// </summary>
    string CacheLocation { get; set; }

    /// <summary>
    /// </summary>
    string CapturedExternalLinksFilename { get; set; }

    /// <summary>
    /// </summary>
    string CapturedSeedUrlsFilename { get; set; }

    /// <summary>
    /// </summary>
    bool CaptureExternalLinks { get; set; }

    /// <summary>
    /// </summary>
    bool CaptureSeedLinks { get; set; }

    /// <summary>
    ///     Option to instruct Spyder to crawl each link in the input file
    /// </summary>
    bool CrawlInputFile { get; set; }

    /// <summary>
    /// </summary>
    bool EnableTagSearch { get; set; }

    /// <summary>
    ///     Crawler will add links from hosts other than seedurl to active crawler
    /// </summary>
    bool FollowExternalLinks { get; set; }

    /// <summary>
    ///     Crawler will add any links from the same host as starting url to active crawler
    /// </summary>
    bool FollowSeedLinks { get; set; }

    /// <summary>
    /// </summary>
    string HtmlTagToSearchFor { get; set; }

    /// <summary>
    /// </summary>
    string InputFileName { get; set; }

    /// <summary>
    /// </summary>
    string[]? LinkPatternExclusions { get; set; }

    /// <summary>
    /// </summary>
    LogLevel LoggingLevel { get; set; }

    /// <summary>
    /// </summary>
    string LogPath { get; set; }

    /// <summary>
    /// </summary>
    string OutputFileName { get; set; }

    /// <summary>
    /// </summary>
    string OutputFilePath { get; set; }

    /// <summary>
    /// </summary>
    int ScrapeDepthLevel { get; set; }

    /// <summary>
    /// </summary>
    bool SeedDomainOnly { get; set; }

    /// <summary>
    /// </summary>
    string StartingUrl { get; set; }

    /// <summary>
    /// </summary>
    bool UseLocalCache { get; set; }
}