using System.Collections.Concurrent;


namespace KC.Apps.Logging ;

    /// <summary>
    ///     A Provider of <see cref="TextFileLogger" /> instances.
    /// </summary>
    [ProviderAlias("TextFileLogging")]
    public class TextFileLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly TextFileFormatter _formatter;
        private readonly ConcurrentDictionary<string, TextFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IDisposable? _onChangeToken;
        private TextFileLoggerConfiguration _currentConfig;
        private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;





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





        public ILogger CreateLogger(string categoryName)
            {
                return _loggers.GetOrAdd(
                    categoryName, name => new TextFileLogger(name, _formatter, _scopeProvider, GetCurrentConfig()));
            }





        public void Dispose()
            {
                _loggers.Clear();
                _onChangeToken?.Dispose();
            }





        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            {
                _scopeProvider = scopeProvider;
                foreach (var logger in _loggers)
                {
                    logger.Value.ScopeProvider = _scopeProvider;
                }
            }





        private TextFileLoggerConfiguration GetCurrentConfig()
            {
                return _currentConfig;
            }
    }