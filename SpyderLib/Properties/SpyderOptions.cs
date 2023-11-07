// ReSharper disable UnusedAutoPropertyAccessor.Global



#region

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.Properties;

/// <summary>
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

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string CacheLocation { get; set; }

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string CapturedExternalLinksFilename { get; set; } = "CapturedExternalLinks.txt";

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string CapturedSeedUrlsFilename { get; set; } = "CapturedSeedUrls.txt";

    /// <summary>
    /// </summary>
    [Required]
    public bool KeepExternalLinks { get; init; }

    /// <summary>
    /// </summary>
    [Required]
    public bool KeepBaseLinks { get; init; }


    /// <summary>
    ///     Concurrent limit on number of active crawlers
    /// </summary>
    public int ConcurrentCrawlingTasks { get; set; } = 5;

    /// <summary>
    ///     Option to instruct Spyder to crawl each link in the input file
    /// </summary>
    /// <returns>Boolean</returns>
    public bool CrawlInputFile { get; init; }

    public bool DownloadTagSource { get; set; }

    /// <summary>
    /// </summary>
    [Required]
    public bool EnableTagSearch { get; init; }

    /// <summary>
    ///     Crawler will add links from hosts other than seed url to active crawler
    /// </summary>
    [Required]
    public bool FollowExternalLinks { get; init; } = false;


    [Required] [AllowNull] public string HtmlTagToSearchFor { get; init; }

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string InputFileName { get; init; }


    /// <summary>
    /// </summary>
    [Required]
    public string[] LinkPatternExclusions { get; set; } = { "?id=", "file://", "mailto://", "?cb=" };

    /// <summary>
    /// </summary>
    [Required]
    public LogLevel LoggingLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// </summary>
    [Required]
    public string LogPath { get; set; } = AppContext.BaseDirectory;

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string OutputFileName { get; set; } = "OutputFilename.txt";

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string OutputFilePath { get; set; } = AppContext.BaseDirectory;

    [Required] public int QueueCapacity { get; set; } = 100;

    /// <summary>
    /// The depth that links will be followed
    /// </summary>
    [Required]
    public int LinkDepthLimit { get; set; } = 2;


    /// <summary>
    /// The Base url or seed url, Depth Level 0
    /// </summary>
    [Url]
    [Required]
    public string StartingUrl { get; set; } = string.Empty;

    /// <summary>
    /// Should we save a copy of the page returned local
    /// It is highly recommended to use a cache when using spyder
    /// in any development or testing environment. In a set and forget
    /// environment it will not improve the performance.
    /// </summary>
    [Required]
    public bool UseLocalCache { get; set; } = false;





    #endregion
}