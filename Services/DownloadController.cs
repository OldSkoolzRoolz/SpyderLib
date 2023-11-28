#region

using System.Diagnostics;

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Services;

public sealed class DownloadController : IDownloadControl
{
    [NotNull] private readonly ICacheIndexService _cache;
    [NotNull] private readonly IBackgroundDownloadQue _downloadQue;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    [NotNull] private readonly ILogger _logger;
    [NotNull] private readonly SpyderOptions _options;
    private HashSet<string> _searchedPages;

    #region Interface Members

    public async Task AddDownloadItem(DownloadItem item)
        {
            await _downloadQue.QueueBackgroundWorkItemAsync(workItem: item).ConfigureAwait(false);
        }





    /// <summary>
    ///     Searches each page in local cache for the htmlElement specified in SpyderOptions
    ///     and creates a downloadItem to the background downloader.
    /// </summary>
    public async Task SearchLocalCacheForHtmlTag()
        {
            // Make the _searchedPages a HashSet for faster exclude check
            _searchedPages = new(await LoadSearchedPagesFromDisk().ConfigureAwait(false));

            var processedAmount = 0;
            var takeAmount = 500;

            foreach (var page in _cache.CacheIndexItems)
                {
                    //Stop processing after reaching 200 items
                    if (processedAmount >= takeAmount)
                        break;

                    //Exclude pages that have been searched before 
                    if (!_searchedPages.Contains(item: page.Value))
                        {
                            _ = _searchedPages.Add(item: page.Value);
                            var pageSource = await GetCachedPageSourceFromDisk(page: page).ConfigureAwait(false);

                            //Checks cache item for HtmlTag and returns any found source links
                            var found = HtmlParser.TryExtractUserTagFromDocument(
                                HtmlParser.CreateHtmlDocument(content: pageSource),
                                tagToSearchFor: _options.HtmlTagToSearchFor,
                                out var foundLinks);

                            //  If there are any links found they are added to the download Que.
                            if (found)
                                {
                                    foreach (var s in foundLinks.Select(k => k.Key))
                                        {
                                            try
                                                {
                                                    await AddDownloadItem(new(link: s,
                                                            savePath: _options.OutputFilePath))
                                                        .ConfigureAwait(false);
                                                }
                                            catch (SpyderException)
                                                {
                                                    _ = _cache.CacheIndexItems.Remove(key: page.Key, value: out _);
                                                }
                                        }
                                }

                            processedAmount++;
                        }
                }

            _logger.SpyderTrace($"Finished Adding download tasks. Que Count {_downloadQue.Count}");
            await SaveSearchedPagesToDisk(searchedPages: _searchedPages).ConfigureAwait(false);
        }





    public void SetInputComplete() { }

    #endregion

    #region Public Methods

    public DownloadController(IOptions<SpyderOptions> options,
        ILoggerFactory logger,
        IBackgroundDownloadQue downloadQue,
        ICacheIndexService cacheIndexService)
        {
            ArgumentNullException.ThrowIfNull(argument: options);

            ArgumentNullException.ThrowIfNull(argument: logger);

            _logger = logger.CreateLogger<DownloadController>();
            _logger.SpyderInfoMessage(message: "Download Controller Initialized!");
            _options = options.Value;
            _downloadQue = downloadQue ?? throw new ArgumentNullException(nameof(downloadQue));
            _cache = cacheIndexService ?? throw new ArgumentNullException(nameof(cacheIndexService));
            _ = StartupComplete.TrySetResult(true);
            Init();
        }





    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion

    #region Private Methods

    private async Task<string> GetCachedPageSourceFromDisk(KeyValuePair<string, string> page)
        {
            var pagePath = Path.Combine(path1: _options.CacheLocation, path2: page.Value);
            var source = await File.ReadAllTextAsync(path: pagePath).ConfigureAwait(false);


            return source;
        }





    private void Init()
        {
            Debug.WriteLine(value: _options.LogPath);
        }





    private async Task<HashSet<string>> LoadSearchedPagesFromDisk()
        {
            var path = Path.Combine(path1: _options.OutputFilePath, path2: "SearchedPages.txt");
            if (!File.Exists(path: path)) return new();
            var set = new HashSet<string>(await File.ReadAllLinesAsync(path: path).ConfigureAwait(false));
            return set;
        }





    private async Task SaveSearchedPagesToDisk(HashSet<string> searchedPages)
        {
            using var file = File.CreateText(Path.Combine(path1: _options.OutputFilePath, path2: "SearchedPages.txt"));
            foreach (var page in searchedPages)
                {
                    await file.WriteLineAsync(value: page).ConfigureAwait(false);
                }
        }

    #endregion
}