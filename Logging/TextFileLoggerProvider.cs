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
    private readonly TextFileFormatter _formatter;
    private readonly ConcurrentDictionary<string, TextFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDisposable _onChangeToken;

    private TextFileLoggerConfiguration _currentConfig;





    /// <summary>
    ///     Creates an instance of <see cref="TextFileLoggerProvider" />
    /// </summary>
    /// <param name="config"></param>
    public TextFileLoggerProvider(IOptionsMonitor<TextFileLoggerConfiguration> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
            _formatter = new TextFileFormatter(config);
        }





    public TextFileLoggerProvider(TextFileLoggerConfiguration options)
        {
            _currentConfig = options;
            _formatter = new TextFileFormatter(options);
        }





    public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }





    public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(
                                     categoryName, name =>
                                         new TextFileLogger(name, _formatter, GetCurrentConfig()));
        }





    private TextFileLoggerConfiguration GetCurrentConfig()
        {
            return _currentConfig;
        }
}