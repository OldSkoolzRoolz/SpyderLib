#region

using Microsoft.Extensions.Logging.Console;

#endregion


namespace KC.Apps.Logging;

public sealed class CustomOptions : ConsoleFormatterOptions
{
    #region Public Methods

    public string CustomPrefix { get; set; }
    public string CustomSuffix { get; set; }

    #endregion
}