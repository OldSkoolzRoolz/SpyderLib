#region

using Microsoft.Extensions.Logging.Console;

#endregion

namespace KC.Apps.SpyderLib.Logging;

public sealed class LogFormatterOptions : ConsoleFormatterOptions
{
    #region Public Methods

    public string CustomPrefix => "~~<{";

    #endregion
}