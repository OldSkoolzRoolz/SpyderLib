#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#endregion

namespace KC.Apps.SpyderLib.Logging;

/// <summary>
///     A logger that writes messages to a text file.
/// </summary>
internal class TextFileLogger : ILogger
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

    #region Interface Members

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
            if (!IsEnabled(logLevel: logLevel))
                {
                    return;
                }

            ArgumentNullException.ThrowIfNull(argument: formatter);
            var logName = GetLogFileName();
            using var tStreamWriter = new StreamWriter(path: logName, true);

            var logEntry = new LogEntry<TState>(logLevel: logLevel, category: _name, eventId: eventId, state: state,
                exception: exception, formatter: formatter);
            this.Formatter.Write(logEntry: in logEntry, textWriter: tStreamWriter);
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
            name = Path.Combine(path1: this.Config.LogLocation, path2: name);


            return name;
        }

    #endregion
}