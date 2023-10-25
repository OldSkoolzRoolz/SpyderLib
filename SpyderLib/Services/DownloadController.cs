#region

using JetBrains.Annotations;
using KC.Apps.Properties;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Services;

public class DownloadController
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    [NotNull] private readonly ILogger<DownloadController> _logger;
    private readonly SpyderOptions _options = SpyderControlService.CrawlOptions;





    public DownloadController(ILogger<DownloadController> logger)
        {
            _logger = logger;
            _logger.LogInformation("Download Controller Initialized!");
            Init();
        }





    private void Init()
        {
            Console.WriteLine(_options.LogPath);
        }
}