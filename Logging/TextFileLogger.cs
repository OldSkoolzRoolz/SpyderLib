#region

#endregion


#region

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#endregion


namespace KC.Apps.Logging;

/// <summary>
///     A logger that writes messages to a text file.
/// </summary>
internal class TextFileLogger : ILogger
{
    private readonly string _name;





    internal TextFileLogger(
        string                      name,
        TextFileFormatter           formatter,
        TextFileLoggerConfiguration config)
        {
            _name = name;
            this.Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            this.Config = config;
        }





    [ThreadStatic] private static StreamWriter _tStreamWriter;

    private TextFileLoggerConfiguration Config { get; }


    private TextFileFormatter Formatter { get; }





    /// <summary>Begins a logical operation scope.</summary>
    /// <param name="state">The identifier for the scope.</param>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <returns>An <see cref="T:System.IDisposable" /> that ends the logical operation scope on dispose.</returns>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return default;
        }





    public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }





    /// <summary>
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="state"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    /// <typeparam name="TState"></typeparam>
    public void Log<TState>(
        LogLevel                        logLevel,
        EventId                         eventId,
        TState                          state,
        Exception                       exception,
        Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                {
                    return;
                }

            ArgumentNullException.ThrowIfNull(formatter);
            var logName = GetLogFileName();
            using (_tStreamWriter = new StreamWriter(logName, true))
                {
                    var logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
                    this.Formatter.Write(in logEntry, _tStreamWriter);
                }
        }





    private string GetLogFileName()
        {
            string name;
            name = this.Config.UseSingleLogFile
                ? "FileLogger-UnifiedLog.log"
                :

                //create separate Log file for each category 
                $"FileLogger-{_name}.log";

            //return path and filename
            name = Path.Combine(this.Config.LogLocation, name);
            return name;
        }
}