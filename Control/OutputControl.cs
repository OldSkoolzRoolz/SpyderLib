#region

using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Services;

#endregion




namespace KC.Apps.SpyderLib.Control;




public static class OutputControl
    {
        #region Instance variables

        private static readonly object s_lock = new();

        #endregion




        #region Prop

        private static SpyderOptions _options => SpyderControlService.CrawlerOptions;

        // These properties store the captured links
        public static ConcurrentScrapedUrlCollection CapturedExternalLinks { get; set; } = new();
        public static ConcurrentScrapedUrlCollection CapturedSeedLinks { get; set; } = new();
        public static ConcurrentScrapedUrlCollection CapturedUrlWithSearchResults { get; set; } = new();
        public static ConcurrentScrapedUrlCollection CapturedVideoLinks { get; set; } = new();
        public static ConcurrentScrapedUrlCollection FailedCrawlerUrls { get; set; } = new();

        // Internal collection used for exclusion purposes
        public static ConcurrentScrapedUrlCollection UrlsScrapedThisSession { get; set; } = new();

        #endregion




        #region Methods

        // This method is called when the library is shutting down
        public static void OnLibraryShutdown()
            {
                var collectionDictionary = new Dictionary<ConcurrentScrapedUrlCollection, string>
                    {
                        { CapturedVideoLinks, "TestingVideoLinks.txt" },
                        { UrlsScrapedThisSession, "AllUrlsCaptured.txt" },
                        { FailedCrawlerUrls, "FailedCrawlerUrls.txt" },
                        { CapturedExternalLinks, _options.CapturedExternalLinksFilename ?? "ExternalLinksTesting.txt" },
                        { CapturedSeedLinks, _options.CapturedSeedUrlsFilename ?? "CapturedSeedUrlsFilename.txt" },
                        { CapturedUrlWithSearchResults, "PositiveTagSearchResults.txt" }
                    };

                foreach (var entry in collectionDictionary)
                    {
                        if (!entry.Key.IsEmpty)
                            {
                                SaveCollectionToFile(entry.Key, entry.Value);
                            }
                    }

                Console.WriteLine("Output written");
            }

        #endregion




        #region Methods

        /// <summary>
        ///     Save the given collection to a file.
        /// </summary>
        /// <param name="col">The collection to save.</param>
        /// <param name="fileName">The name of the file to save to.</param>
        private static void SaveCollectionToFile(ConcurrentScrapedUrlCollection col, string fileName)
            {
                if (col is not { IsEmpty: true })
                    {
                        lock (s_lock)
                            {
                                var path = Path.Combine(_options.OutputFilePath, fileName);
                                using var sw = File.CreateText(path);
                                foreach (var item in col)
                                    {
                                        sw.WriteLine(item.Key);
                                    }
                            }
                    }
            }

        #endregion
    }