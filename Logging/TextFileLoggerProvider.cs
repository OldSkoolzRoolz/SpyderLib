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
        #region Instance variables

        private TextFileLoggerConfiguration _currentConfig;
        private readonly TextFileFormatter _formatter;
        private readonly ConcurrentDictionary<string, TextFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IDisposable? _onChangeToken;

        #endregion





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





        #region Methods

        public ILogger CreateLogger(string categoryName)
            {
                return _loggers.GetOrAdd(
                    categoryName, name => new TextFileLogger(name, _formatter, GetCurrentConfig()));
            }





        public void Dispose()
            {
                _loggers.Clear();
                _onChangeToken?.Dispose();
            }

        #endregion




        #region Methods

        private TextFileLoggerConfiguration GetCurrentConfig()
            {
                return _currentConfig;
            }

        #endregion
    }