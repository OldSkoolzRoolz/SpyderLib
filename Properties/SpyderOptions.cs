// ReSharper disable UnusedAutoPropertyAccessor.Global

#region

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace SpyderLib.Properties;

/// <summary>
/// </summary>
public class SpyderOptions : IOptions<SpyderOptions>
{
    public SpyderOptions()
    {
        ValidateCrawlerOptions();
    }





    /// <summary>
    /// </summary>
    [Required]
    [AllowNull]
    public string CacheLocation { get; set; } = Directory.GetCurrentDirectory();

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

    #region Methods

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public List<string> ValidateCrawlerOptions()
    {
        var errors = new List<string>();



        if (string.IsNullOrEmpty(value: this.LogPath) || string.IsNullOrEmpty(value: this.OutputFilePath))
        {
            errors.Add(item: "Check LogPath and OutputPath ");
        }

        if (string.IsNullOrEmpty(value: this.CapturedSeedUrlsFilename) && this.CaptureSeedLinks)
        {
            errors.Add(item: "Captured seed filename must not be null when CaptureSeeds is enabled..");
        }

        if (string.IsNullOrEmpty(value: this.CapturedExternalLinksFilename) && this.CaptureExternalLinks)
        {
            errors.Add(
                       item: "Captured External filename must not be null when Capture External links is enabled..");
        }

        if (this.ScrapeDepthLevel <= 1)
        {
            errors.Add(item: "Your depth level is low, verify your setting is correct");
        }

        return errors;
    }

    #endregion

    /// <summary>Gets the default configured <typeparamref name="TOptions" /> instance.</summary>
    public SpyderOptions Value { get; }
}