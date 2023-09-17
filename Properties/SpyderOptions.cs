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
    public bool CaptureExternalLinks { get; set; }

    /// <summary>
    /// </summary>
    [Required]
    public bool CaptureSeedLinks { get; set; }

    /// <summary>
    ///     Option to instruct Spyder to crawl each link in the input file
    /// </summary>
    /// <returns>Boolean</returns>
    public bool CrawlInputFile { get; set; }

    /// <summary>
    /// </summary>
    [Required]
    public bool EnableTagSearch { get; set; }

    /// <summary>
    ///     Crawler will add links from hosts other than seedurl to active crawler
    /// </summary>
    [Required]
    public bool FollowExternalLinks { get; set; }

    /// <summary>
    ///     Crawler will add any links from the same host as starting url to active crawler
    /// </summary>
    [Required]
    public bool FollowSeedLinks { get; set; }

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string HtmlTagToSearchFor { get; set; } = "";

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string InputFileName { get; set; } = "";

    /// <summary>
    /// </summary>
    [Required]
    public string[]? LinkPatternExclusions { get; set; } = { "?id=", "file://", "mailto://", "?cb=" };

    /// <summary>
    /// </summary>
    [Required]
    public LogLevel LoggingLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// </summary>
    [Required]
    public string LogPath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string OutputFileName { get; set; } = "OutputFilename.txt";

    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string OutputFilePath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// </summary>
    [Required]
    public int ScrapeDepthLevel { get; set; } = 3;

    /// <summary>
    /// </summary>
    [Required]
    public bool SeedDomainOnly { get; set; }

    /// <summary>
    /// </summary>
    [Url]
    [Required]
    public string StartingUrl { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    [Required]
    public bool UseLocalCache { get; set; }

    [Required] public int QueueCapacity { get; set; }





    /// <summary>
    /// </summary>
    /// <returns></returns>
    public List<string> ValidateInitialOptions()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(this.LogPath) || string.IsNullOrEmpty(this.OutputFilePath))
            {
                errors.Add("Check LogPath and OutputPath ");
            }

            if (string.IsNullOrEmpty(this.CapturedSeedUrlsFilename) && this.CaptureSeedLinks)
            {
                errors.Add("Captured seed filename must not be null when CaptureSeeds is enabled..");
            }

            if (string.IsNullOrEmpty(this.CapturedExternalLinksFilename) && this.CaptureExternalLinks)
            {
                errors.Add(
                    "Captured External filename must not be null when Capture External links is enabled..");
            }

            if (this.ScrapeDepthLevel <= 1)
            {
                errors.Add("Your depth level is low, verify your setting is correct");
            }

            return errors;
        }
}