using Microsoft.Extensions.Logging.Console;



namespace KC.Apps.SpyderLib.Logging;

public sealed class CustomOptions : ConsoleFormatterOptions
{
    #region Properteez

    public string CustomPrefix { get; set; }
    public string CustomSuffix { get; set; }

    #endregion
}