#region

using Microsoft.Extensions.Logging.Console;

#endregion


namespace KC.Apps.Logging;

public class CustomColorOptions : SimpleConsoleFormatterOptions
{
    #region Public Methods

    public string CustomPrefix { get; set; }

    #endregion
}