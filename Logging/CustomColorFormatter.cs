#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.Logging;

public sealed class CustomColorFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable _optionsReloadToken;

    private CustomColorOptions _formatterOptions;





    public CustomColorFormatter(IOptionsMonitor<CustomColorOptions> options)

        // Case insensitive
        : base("customName")
        {
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(ReloadLoggerOptions), options.CurrentValue);
        }





    public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }





    private bool ConsoleColorFormattingEnabled =>
        _formatterOptions.ColorBehavior == LoggerColorBehavior.Enabled ||
        _formatterOptions.ColorBehavior == LoggerColorBehavior.Default;





    public override void Write<TState>(
        in LogEntry<TState>    logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter             textWriter)
        {
            if (logEntry.Exception is null)
                {
                    return;
                }

            var message =
                logEntry.Formatter.Invoke(
                                          logEntry.State, logEntry.Exception);

            CustomLogicGoesHere(textWriter);
            textWriter.WriteLine(message);
        }





    private void ReloadLoggerOptions(CustomColorOptions options)
        {
            _formatterOptions = options;
        }





    private void CustomLogicGoesHere(TextWriter textWriter)
        {
            if (this.ConsoleColorFormattingEnabled)
                {
                    textWriter.WriteWithColor(
                                              _formatterOptions.CustomPrefix ?? string.Empty,
                                              ConsoleColor.Black,
                                              ConsoleColor.Green);
                }
            else
                {
                    textWriter.Write(_formatterOptions.CustomPrefix);
                }
        }
}