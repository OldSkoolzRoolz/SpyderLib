#region

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Download que
/// </summary>
public class DownloadQue
{
    private readonly ILogger _logger;





    public DownloadQue(ILogger logger)
    {
        _logger = logger;
    }
}