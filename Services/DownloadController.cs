#region

using JetBrains.Annotations;

using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Services;

public abstract class DownloadController
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    [NotNull] private readonly ILogger _logger;
    private readonly SpyderOptions _options;

    #region Public Methods

    public DownloadController(SpyderOptions options, ILogger logger)
        {
            _logger = logger;
            _logger.LogInformation("Download Controller Initialized!");
            _options = options;
            StartupComplete.TrySetResult(true);
            Init();
        }





    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion

    #region Private Methods

    private void Init()
        {
            Console.WriteLine(_options.LogPath);
        }

    #endregion
}