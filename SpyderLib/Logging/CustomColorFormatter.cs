#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.Logging;

public sealed class CustomColorFormatter : ConsoleFormatter, IDisposable
{
    #region Other Fields

    private readonly IDisposable _optionsReloadToken;

    private CustomColorOptions _formatterOptions;

    #endregion

    #region Properteez

    private bool ConsoleColorFormattingEnabled =>
        _formatterOptions.ColorBehavior == LoggerColorBehavior.Enabled ||
        _formatterOptions.ColorBehavior == LoggerColorBehavior.Default;

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
        : base("customName")
        {
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(ReloadLoggerOptions), options.CurrentValue);
        }





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

    #endregion

    #region Private Methods

    private void ReloadLoggerOptions(
        CustomColorOptions options)
        {
            _formatterOptions = options;
        }





    private void CustomLogicGoesHere(
        TextWriter textWriter)
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

    #endregion
}