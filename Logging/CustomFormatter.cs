﻿#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

#endregion

namespace KC.Apps.SpyderLib.Logging;

public sealed class CustomFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable _optionsReloadToken;
    private CustomOptions _formatterOptions;

    #region Interface Members

    public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

    #endregion

    #region Public Methods

    public CustomFormatter(
            IOptionsMonitor<CustomOptions> options)

        // Case insensitive
        : base("customName")
        {
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(ReloadLoggerOptions), options.CurrentValue);
        }





    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
        {
            var message =
                logEntry.Formatter.Invoke(
                    logEntry.State, logEntry.Exception);

            CustomLogicGoesHere(textWriter);
            textWriter.WriteLine(message);
        }

    #endregion

    #region Private Methods

    private void ReloadLoggerOptions(
        CustomOptions options)
        {
            _formatterOptions = options;
        }





    private void CustomLogicGoesHere(
        TextWriter textWriter)
        {
            textWriter.Write(_formatterOptions.CustomPrefix);
        }

    #endregion
}