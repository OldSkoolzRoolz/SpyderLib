#region

using Microsoft.Extensions.Logging.Console;

#endregion


namespace KC.Apps.Logging;

public class CustomColorOptions : SimpleConsoleFormatterOptions
{
    public string CustomPrefix { get; set; }
}