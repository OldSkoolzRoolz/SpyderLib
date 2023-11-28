#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Logging;

public sealed class CustomColorFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable _optionsReloadToken;
    private CustomColorOptions _formatterOptions;

    #region Properteez

    private bool ConsoleColorFormattingEnabled =>
        _formatterOptions.ColorBehavior is LoggerColorBehavior.Enabled or LoggerColorBehavior.Default;

    #endregion

    #region Interface Members

    public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

    #endregion

    #region Public Methods

    public CustomColorFormatter(
            IOptionsMonitor<CustomColorOptions> options)

        // Case insensitive
        : base(name: "customName")
        {
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(listener: ReloadLoggerOptions), options.CurrentValue);
        }





    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
        {
            if (logEntry.Exception is null)
                {
                    return;
                }

            var message =
                logEntry.Formatter.Invoke(
                    arg1: logEntry.State, arg2: logEntry.Exception);

            CustomLogicGoesHere(textWriter: textWriter);
            textWriter.WriteLine(value: message);
        }

    #endregion

    #region Private Methods

    private void CustomLogicGoesHere(
        TextWriter textWriter)
        {
            if (this.ConsoleColorFormattingEnabled)
                {
                    textWriter.WriteWithColor(
                        _formatterOptions.CustomPrefix ?? string.Empty,
                        background: ConsoleColor.Black,
                        foreground: ConsoleColor.Green);
                }
            else
                {
                    textWriter.Write(value: _formatterOptions.CustomPrefix);
                }
        }





    private void ReloadLoggerOptions(
        CustomColorOptions options)
        {
            _formatterOptions = options;
        }

    #endregion
}