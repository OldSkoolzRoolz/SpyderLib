using Microsoft.Extensions.Logging.Console;



namespace KC.Apps.SpyderLib.Logging;

public sealed class LogFormatterOptions : ConsoleFormatterOptions
{
    #region Properteez

    public static string CustomPrefix => "~~<{";

    #endregion
}