using System.Collections.Concurrent;
using System.Runtime.Versioning;

using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;



namespace KC.Apps.SpyderLib.Logging;

/// <summary>
///     A Provider of <see cref="TextFileLogger" /> instances.
/// </summary>
[UnsupportedOSPlatform(platformName: "browser")]
[ProviderAlias(alias: "TextFileLogger")]
public class TextFileLoggerProvider : ILoggerProvider
{
    private readonly TextFileLoggerConfiguration _currentConfig;
    private bool _disposed;
    private readonly TextFileFormatter _formatter;

    private readonly ConcurrentDictionary<string, TextFileLogger> _loggers =
        new(comparer: StringComparer.OrdinalIgnoreCase);






    /// <summary>
    ///     Creates an instance of <see cref="TextFileLoggerProvider" />
    /// </summary>
    /// <param name="config"></param>
    public TextFileLoggerProvider(IOptions<TextFileLoggerConfiguration> config)
        {
            Guard.IsNotNull(value: config);
            _currentConfig = config.Value;
            _formatter = new(config: _currentConfig);
        }






    #region Public Methods

    public ILogger CreateLogger(
        string categoryName)
        {
            return _loggers.GetOrAdd(
                key: categoryName, name =>
                    new(name: name, formatter: _formatter, GetCurrentConfig()));
        }






    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    #endregion






    #region Private Methods

    protected virtual void Dispose(
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