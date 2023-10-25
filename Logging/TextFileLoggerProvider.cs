#region

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.Logging;

/// <summary>
///     A Provider of <see cref="TextFileLogger" /> instances.
/// </summary>
[ProviderAlias("TextFileLogging")]
public class TextFileLoggerProvider : ILoggerProvider
{
    private readonly IOptions<TextFileLoggerConfiguration> _currentConfig;
    private readonly TextFileFormatter _formatter;
    private readonly ConcurrentDictionary<string, TextFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;





    /// <summary>
    ///     Creates an instance of <see cref="TextFileLoggerProvider" />
    /// </summary>
    /// <param name="config"></param>
    public TextFileLoggerProvider(IOptions<TextFileLoggerConfiguration> config)
        {

            _formatter = new TextFileFormatter(config);
            _currentConfig = config;
        }





    public TextFileLoggerProvider(TextFileLoggerConfiguration options)
        {

            _formatter = new TextFileFormatter(options);
        }





    public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }





    protected virtual void Dispose(bool disposing)
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





    public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(
                                     categoryName, name =>
                                         new TextFileLogger(name, _formatter, GetCurrentConfig()));
        }





    private TextFileLoggerConfiguration GetCurrentConfig()
        {
            return _currentConfig.Value;
        }
}