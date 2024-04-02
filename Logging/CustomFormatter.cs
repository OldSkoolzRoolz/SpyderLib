using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;



namespace KC.Apps.SpyderLib.Logging;

public sealed class CustomFormatter : ConsoleFormatter, IDisposable
{
    #region feeeldzzz

    private readonly IDisposable _optionsReloadToken;
    private CustomOptions _formatterOptions;

    #endregion






    public CustomFormatter(
            IOptionsMonitor<CustomOptions> options)

        // Case insensitive
        : base("customName")
    {
        Guard.IsNotNull(options);
        (_optionsReloadToken, _formatterOptions) =
            (options.OnChange(ReloadLoggerOptions), options.CurrentValue);
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
        ArgumentNullException.ThrowIfNull(textWriter);
        var message =
            logEntry.Formatter.Invoke(
                logEntry.State, logEntry.Exception);

        CustomLogicGoesHere(textWriter);
        textWriter.WriteLine(message);
    }

    #endregion






    #region Private Methods

    private void CustomLogicGoesHere(
        TextWriter textWriter)
    {
        textWriter.Write(_formatterOptions.CustomPrefix);
    }






    private void ReloadLoggerOptions(
        CustomOptions options)
    {
        _formatterOptions = options;
    }

    #endregion
}