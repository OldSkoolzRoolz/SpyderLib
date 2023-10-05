// ReSharper disable UnusedAutoPropertyAccessor.Global




#region

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

#endregion




namespace KC.Apps.SpyderLib;




/// <summary>
/// </summary>
public class SpyderOptions
    {
        #region Prop

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
        public bool CaptureExternalLinks { get; init; }

        /// <summary>
        /// </summary>
        [Required]
        public bool CaptureSeedLinks { get; init; }

        /// <summary>
        ///     Option to instruct Spyder to crawl each link in the input file
        /// </summary>
        /// <returns>Boolean</returns>
        public bool CrawlInputFile { get; init; }

        /// <summary>
        /// </summary>
        [Required]
        public bool EnableTagSearch { get; init; }

        /// <summary>
        ///     Crawler will add links from hosts other than seedurl to active crawler
        /// </summary>
        [Required]
        public bool FollowExternalLinks { get; init; }

        /// Crawler will add any links from the same host as starting url to active crawler
        /// </summary>
        /// <summary>
        /// </summary>
        [Required]
        [AllowNull]
        public string HtmlTagToSearchFor { get; init; }

        /// <summary>
        /// </summary>
        [Required]
        [AllowNull]
        public string InputFileName { get; init; }

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
        /// </summary>
        [Required]
        public int ScrapeDepthLevel { get; set; } = 2;


        /// <summary>
        /// </summary>
        [Url]
        [Required]
        public string StartingUrl { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [Required]
        public bool UseLocalCache { get; set; } = false;

        #endregion




        #region Methods

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

        #endregion




        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum HtmlTagList
            {
                video,
                img
            }
    }