using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;



namespace KC.Apps.SpyderLib.Logging;

/// <summary>
///     A logger that writes messages to a text file.
/// </summary>
internal sealed class TextFileLogger : ILogger
{
    private readonly string _name;






    internal TextFileLogger(
        string name,
        TextFileFormatter formatter,
        TextFileLoggerConfiguration config)
        {
            _name = name;
            this.Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            this.Config = config;
        }






    #region Properteez

    private TextFileLoggerConfiguration Config { get; }
    private TextFileFormatter Formatter { get; }

    #endregion






    #region Public Methods

    /// <summary>Begins a logical operation scope.</summary>
    /// <param name="state">The identifier for the scope.</param>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <returns>An <see cref="System.IDisposable" /> that ends the logical operation scope on dispose.</returns>
    public IDisposable BeginScope<TState>(
        TState state) where TState : notnull
        {
            return default;
        }






    public bool IsEnabled(
        LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }






    /// <summary>
    ///     Log entry
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="state"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    /// <typeparam name="TState"></typeparam>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                {
                    return;
                }

            ArgumentNullException.ThrowIfNull(formatter);
            var logName = GetLogFileName();
            using var tStreamWriter = new StreamWriter(logName, true);

            var logEntry = new LogEntry<TState>(logLevel, _name, eventId, state,
                exception, formatter);
            this.Formatter.Write(in logEntry, tStreamWriter);
        }

    #endregion






    #region Private Methods

    private string GetLogFileName()
        {
            var name = this.Config.UseSingleLogFile
                ? "FileLogger-UnifiedLog.log"
                :
                //create separate Log file for each category 
                $"FileLogger-{_name}.log";

            //return path and filename
            name = Path.Combine(this.Config.LogLocation, name);


            return name;
        }

    #endregion
}