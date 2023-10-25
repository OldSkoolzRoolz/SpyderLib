#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.Logging;

public sealed class CustomFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable _optionsReloadToken;

    private CustomOptions _formatterOptions;





    public CustomFormatter(IOptionsMonitor<CustomOptions> options)

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





    public override void Write<TState>(
        in LogEntry<TState>    logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter             textWriter)
        {
            var message =
                logEntry.Formatter.Invoke(
                                          logEntry.State, logEntry.Exception);

            CustomLogicGoesHere(textWriter);
            textWriter.WriteLine(message);
        }





    private void ReloadLoggerOptions(CustomOptions options)
        {
            _formatterOptions = options;
        }





    private void CustomLogicGoesHere(TextWriter textWriter)
        {
            textWriter.Write(_formatterOptions.CustomPrefix);
        }
}