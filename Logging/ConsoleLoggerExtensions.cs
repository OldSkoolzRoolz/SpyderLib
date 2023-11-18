#region

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Logging;

public static class ConsoleLoggerExtensions
{
    #region Public Methods

    public static ILoggingBuilder AddCustomFormatter(
        this ILoggingBuilder builder,
        Action<CustomOptions> configure)
        {
            return builder.AddConsole(options => options.FormatterName = "customName")
                .AddConsoleFormatter<CustomFormatter, CustomOptions>(configure);
        }

    #endregion
}