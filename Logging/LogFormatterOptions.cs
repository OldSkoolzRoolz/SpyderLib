#region

using Microsoft.Extensions.Logging.Console;

#endregion


namespace KC.Apps.Logging;

public sealed class LogFormatterOptions : ConsoleFormatterOptions
{
    public string CustomPrefix { get; set; } = "~~<{";
}