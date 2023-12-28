using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;



namespace KC.Apps.SpyderLib.Logging;

public sealed class CustomFormatter : ConsoleFormatter, IDisposable
{
    private CustomOptions _formatterOptions;
    private readonly IDisposable _optionsReloadToken;






    public CustomFormatter(
            IOptionsMonitor<CustomOptions> options)

        // Case insensitive
        : base(name: "customName")
        {
            Guard.IsNotNull(value: options);
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(listener: ReloadLoggerOptions), options.CurrentValue);
        }






    #region Public Methods

    public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }






    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
        {
            ArgumentNullException.ThrowIfNull(argument: textWriter);
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
            textWriter.Write(value: _formatterOptions.CustomPrefix);
        }






    private void ReloadLoggerOptions(
        CustomOptions options)
        {
            _formatterOptions = options;
        }

    #endregion
}