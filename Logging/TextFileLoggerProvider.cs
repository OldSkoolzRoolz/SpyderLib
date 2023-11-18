#region

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Logging;

/// <summary>
///     A Provider of <see cref="TextFileLogger" /> instances.
/// </summary>
[ProviderAlias("TextFileLogging")]
public sealed class TextFileLoggerProvider : ILoggerProvider
{
    private readonly TextFileLoggerConfiguration _currentConfig;
    private readonly TextFileFormatter _formatter;
    private readonly ConcurrentDictionary<string, TextFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    #region Interface Members

    public void Dispose()
        {
            Dispose(true);
        }





    public ILogger CreateLogger(
        string categoryName)
        {
            return _loggers.GetOrAdd(
                categoryName, name =>
                    new TextFileLogger(name, _formatter, GetCurrentConfig()));
        }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Creates an instance of <see cref="TextFileLoggerProvider" />
    /// </summary>
    /// <param name="config"></param>
    public TextFileLoggerProvider(
        TextFileLoggerConfiguration config)
        {
            _formatter = new TextFileFormatter(config);
            _currentConfig = config;
        }

    #endregion

    #region Private Methods

    private void Dispose(
        bool disposing)
        {
            if (_disposed)
                {
                    return;
                }

            if (disposing)
                {
                    _loggers.Clear();
                }

            // Clean up unmanaged resources here if required.

            _disposed = true;
        }





    private TextFileLoggerConfiguration GetCurrentConfig()
        {
            return _currentConfig;
        }

    #endregion
}