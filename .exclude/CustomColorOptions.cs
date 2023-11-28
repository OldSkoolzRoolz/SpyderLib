#region

using Microsoft.Extensions.Logging.Console;

#endregion

namespace KC.Apps.SpyderLib.Logging;

public class CustomColorOptions : SimpleConsoleFormatterOptions
{
    #region Public Methods

    public string CustomPrefix => "<<<";

    #endregion
}