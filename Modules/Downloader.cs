#region

using Microsoft.Extensions.Logging;

#endregion

namespace SpyderLib.Modules;

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