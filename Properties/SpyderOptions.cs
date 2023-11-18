// ReSharper disable UnusedAutoPropertyAccessor.Global

#region

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Properties;

/// <summary>
///     Spyder crawler options
/// </summary>
public class SpyderOptions
{
    #region Public Enums

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum HtmlTagList
    {
        video,
        img
    }

    #endregion

    #region Public Methods

    [Required] [AllowNull] public string CacheLocation { get; init; }
    [Required] [AllowNull] public string CapturedExternalLinksFilename { get; init; } = "CapturedExternalLinks.txt";
    [Required] [AllowNull] public string CapturedSeedUrlsFilename { get; init; } = "CapturedSeedUrls.txt";

    /// <summary>
    ///     Concurrent limit on number of active crawlers
    /// </summary>
    public int ConcurrentCrawlingTasks { get; set; } = 5;

    /// <summary>
    ///     Option to instruct Spyder to crawl each link in the input file
    /// </summary>
    /// <returns>Boolean</returns>
    public bool CrawlInputFile { get; init; }

    public bool DownloadTagSource { get; init; }
    [Required] public bool EnableTagSearch { get; init; }

    /// <summary>
    ///     Crawler will add links from hosts other than seed url to active crawler
    /// </summary>
    [Required]
    public bool FollowExternalLinks { get; init; }

    [Required] [AllowNull] public string HtmlTagToSearchFor { get; init; }
    [Required] [AllowNull] public string InputFileName { get; init; }
    [Required] public bool KeepBaseLinks { get; init; }
    [Required] public bool KeepExternalLinks { get; init; }

public bool UseMetrics {get;init;}

    /// <summary>
    ///     The depth that links will be followed
    /// </summary>
    [Required]
    public int LinkDepthLimit { get; init; } = 2;

    [Required] public string[] LinkPatternExclusions { get; init; } = { "?id=", "file://", "mailto://", "?cb=" };
    [Required] public LogLevel LoggingLevel { get; init; } = LogLevel.Information;
    [Required] public string LogPath { get; init; } = AppContext.BaseDirectory;
    [Required] [AllowNull] public string OutputFileName { get; init; } = "OutputFilename.txt";
    [Required] [AllowNull] public string OutputFilePath { get; init; } = AppContext.BaseDirectory;
    [Required] public int QueueCapacity { get; set; } = 100;

    /// <summary>
    ///     The Base url or seed url, Depth Level 0
    /// </summary>
    [Url]
    [Required]
    public string StartingUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Should we save a copy of the page returned local
    ///     It is highly recommended to use a cache when using spyder
    ///     in any development or testing environment. In a set and forget
    ///     environment it will not improve the performance.
    /// </summary>
    [Required]
    public bool UseLocalCache { get; init; }

    #endregion
}