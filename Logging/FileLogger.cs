#region

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;

#endregion

namespace SpyderLib.Logging;

/// <summary>
///     The log output which all <see cref="FileLogger" /> share to log messages to
/// </summary>
public class FileLoggingOutput : IDisposable
{
    private readonly TimeSpan _flushInterval =
        Debugger.IsAttached ? TimeSpan.FromMilliseconds(10) : TimeSpan.FromSeconds(1);

    private DateTime _lastFlush = DateTime.UtcNow;
    private readonly object _lockObj = new();
    private readonly string _logFileName;
    private StreamWriter _logOutput;
    private static readonly ConcurrentDictionary<FileLoggingOutput, object> Instances = new();





    /// <summary>
    ///     Initializes a new instance of the <see cref="FileLoggingOutput" /> class.
    /// </summary>
    /// <param name="fileName">Name of the log file.</param>
    public FileLoggingOutput(string fileName)
    {
        _logFileName = fileName;
        _logOutput = new(
                         File.Open(path: fileName, mode: FileMode.Append, access: FileAccess.Write,
                                   share: FileShare.ReadWrite),
                         encoding: Encoding.UTF8);
        Instances[this] = null;
    }





    /// <summary>
    ///     Initializes static members of the <see cref="FileLoggingOutput" /> class.
    /// </summary>
    static FileLoggingOutput()
    {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

        static void CurrentDomainProcessExit(object? sender, EventArgs args)
        {
            foreach (var instance in Instances)
            {
                instance.Key.Dispose();
            }
        }
    }





    #region Methods

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
    }





    private void Dispose(bool disposing)
    {
        try
        {
            lock (_lockObj)
            {
                if (_logOutput is not { } output)
                {
                    return;
                }

                _logOutput = null;
                _ = Instances.TryRemove(this, value: out _);

                // Dispose the output, which will flush all buffers.
                output.Dispose();
            }
        }
        catch (Exception? exc)
        {
            var msg = string.Format(format: "Ignoring error closing log file {0} - {1}", arg0: _logFileName,
                                    LogFormatter.PrintException(exception: exc));
            Console.WriteLine(value: msg);
        }
    }





    private static string FormatMessage(
        DateTime   timestamp,
        LogLevel   logLevel,
        string     caller,
        string     message,
        Exception? exception,
        EventId    errorCode)
    {
        if (logLevel == LogLevel.Error)
        {
            message = "!!!!!!!!!! " + message;
        }

        var exc = LogFormatter.PrintException(exception: exception);
        var msg = string.Format(format: "[{0} {1}\t{2}\t{3}\t{4}]\t{5}\t{6}",
                                LogFormatter.PrintDate(date: timestamp), //0
                                Thread.CurrentThread.ManagedThreadId, //1
                                logLevel.ToString(), //2
                                errorCode.ToString(), //3
                                caller, //4
                                message, //5
                                exc); //6

        return msg;
    }





    /// <summary>
    ///     Logs a message.
    /// </summary>
    /// <typeparam name="TState">The type of <paramref name="state" />.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="state">The state.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="formatter">The formatter.</param>
    /// <param name="category">The category.</param>
    internal void Log<TState>(LogLevel  logLevel,  EventId eventId, TState state, Exception? exception,
        Func<TState, Exception, string> formatter, string  category)
    {
        if (exception != null)
        {
            var logMessage = FormatMessage(timestamp: DateTime.UtcNow, logLevel: logLevel, caller: category,
                                           formatter(arg1: state, arg2: exception), exception: exception,
                                           errorCode: eventId);
            lock (_lockObj)
            {
                if (_logOutput == null)
                {
                    return;
                }

                _logOutput.WriteLine(value: logMessage);
                var now = DateTime.UtcNow;
                if (now - _lastFlush > _flushInterval)
                {
                    _lastFlush = now;
                    _logOutput.Flush();
                }
            }
        }
    }

    #endregion
}

/// <summary>
///     File logger, which logs messages to a file.
/// </summary>
public class FileLogger : ILogger
{
    // Class Variables
    private readonly string _category;
    private readonly FileLoggingOutput _output;





    /// <summary>
    ///     Initializes a new instance of the FileLogger class.
    /// </summary>
    /// <param name="output">The output logger.</param>
    /// <param name="category">The category.</param>
    public FileLogger(FileLoggingOutput output, string category)
    {
        _category = category;
        _output = output;
    }





    // Returns a disposable interface that defines a scope for logging messages
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }





    /// Check if certain level of logging is enabled
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }





    /// Logging method with multiple parameters
    public void Log<TState>(LogLevel    logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception, string> formatter)
    {
        //Debug.Assert(exception != null, nameof(exception) + " != null");
        _output.Log(logLevel: logLevel, eventId: eventId, state: state, exception: exception, formatter: formatter,
                    category: _category);
    }





    // Inner class that represents a scope with no operation
    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        #region Methods

        public void Dispose()
        {
        }

        #endregion
    }
}