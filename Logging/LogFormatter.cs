#region

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

#endregion


namespace KC.Apps.Logging;

public class LogFormatter : ConsoleFormatter, IDisposable
{
    private readonly LogFormatterOptions _formatterOptions;





    public LogFormatter(LogFormatterOptions options) : base("SpyderFormatter")
        {
            _formatterOptions = options;
        }





    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
        {
            // TODO release managed resources here
        }





    private const string
        DATE_FORMAT =
            "yyyy-MM-dd " +
            TIME_FORMAT; // Example: 2010-09-02 09:50:43.341 GMT - Variant of UniversalSorta­bleDateTimePat­tern

    internal const int MAX_LOG_MESSAGE_SIZE = 20000;
    private static readonly ConcurrentDictionary<Type, Func<Exception, string>> SExceptionDecoders = new();
    private const string TIME_FORMAT = "HH:mm:ss.fff 'GMT'"; // Example: 09:50:43.341 GMT





    /// <summary>Writes the log message to the specified TextWriter.</summary>
    /// <remarks>
    ///     if the formatter wants to write colors to the console, it can do so by embedding ANSI color codes into the string
    /// </remarks>
    /// <param name="logEntry">The log entry.</param>
    /// <param name="scopeProvider">The provider of scope data.</param>
    /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public override void Write<TState>(
        in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            var logLevel = logEntry.LogLevel;
            var logMessage = logEntry.Formatter(logEntry.State, logEntry.Exception);

            // Format the log message
            var formattedMessage = $"{PrintTime(DateTime.Now)} {logLevel}: {logMessage}";


            // If exception exists, include it in the message 
            if (logEntry.Exception != null)
                {
                    formattedMessage += Environment.NewLine + PrintException(logEntry.Exception);
                }

            // Prepend custom prefix from formatter options, if it exists
            if (!string.IsNullOrEmpty(_formatterOptions.CustomPrefix))
                {
                    formattedMessage = _formatterOptions.CustomPrefix + formattedMessage;
                }

            // Write the log message to the provided TextWriter
            textWriter.WriteLine(formattedMessage);
        }





    /// <summary>
    ///     Parses a date.
    /// </summary>
    /// <param name="dateStr">The date string.</param>
    /// <returns>The parsed date.</returns>
    internal static DateTime ParseDate(string dateStr)
        {
            return DateTime.ParseExact(dateStr, DATE_FORMAT, CultureInfo.InvariantCulture);
        }





    /// <summary>
    ///     Utility function to convert a <c>DateTime</c> object into printable data format used by the Logger subsystem.
    /// </summary>
    /// <param name="date">The <c>DateTime</c> value to be printed.</param>
    /// <returns>Formatted string representation of the input data, in the printable format used by the Logger subsystem.</returns>
    internal static string PrintDate(DateTime date)
        {
            return date.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
        }





    /// <summary>
    ///     Utility function to convert an exception into printable format, including expanding and formatting any nested
    ///     sub-expressions.
    /// </summary>
    /// <param name="exception">The exception to be printed.</param>
    /// <returns>
    ///     Formatted string representation of the exception, including expanding and formatting any nested
    ///     sub-expressions.
    /// </returns>
    internal static string PrintException(Exception exception)
        {
            if (exception == null)
                {
                    return "";
                }

            var sb = new StringBuilder();
            PrintException_Helper(sb, exception, 0);
            return sb.ToString();
        }





    /// <summary>
    ///     Utility function to convert a <c>DateTime</c> object into printable time format used by the Logger subsystem.
    /// </summary>
    /// <param name="date">The <c>DateTime</c> value to be printed.</param>
    /// <returns>Formatted string representation of the input data, in the printable format used by the Logger subsystem.</returns>
    internal static string PrintTime(DateTime date)
        {
            return date.ToString(TIME_FORMAT, CultureInfo.InvariantCulture);
        }





    /// <summary>
    ///     Configures the exception decoder for the specified exception type.
    /// </summary>
    /// <param name="exceptionType">The exception type to configure a decoder for.</param>
    /// <param name="decoder">The decoder.</param>
    internal static void SetExceptionDecoder(Type exceptionType, Func<Exception, string> decoder)
        {
            SExceptionDecoders.TryAdd(exceptionType, decoder);
        }





    private static void PrintException_Helper(StringBuilder sb, Exception exception, int level)
        {
            while (true)
                {
                    if (exception == null)
                        {
                            return;
                        }

                    var message = SExceptionDecoders.TryGetValue(exception.GetType(), out var decoder)
                        ? decoder(exception)
                        : exception.Message;

                    sb.Append($"{Environment.NewLine}Exc level {level}: {exception.GetType()}: {message}");
                    if (exception.StackTrace is { } stack)
                        {
                            sb.Append($"{Environment.NewLine}{stack}");
                        }

                    if (exception is ReflectionTypeLoadException typeLoadException)
                        {
                            var loaderExceptions = typeLoadException.LoaderExceptions;
                            if (loaderExceptions.Length == 0)
                                {
                                    sb.Append("No LoaderExceptions found");
                                }
                            else
                                {
                                    foreach (var inner in loaderExceptions)

                                        // call recursively on all loader exceptions. Same level for all.
                                        {
                                            PrintException_Helper(sb, inner, level + 1);
                                        }
                                }
                        }
                    else if (exception.InnerException != null)
                        {
                            if (exception is AggregateException { InnerExceptions: { Count: > 1 } innerExceptions })
                                {
                                    foreach (var inner in innerExceptions)

                                        // call recursively on all inner exceptions. Same level for all.
                                        {
                                            PrintException_Helper(sb, inner, level + 1);
                                        }
                                }
                            else
                                {
                                    // call recursively on a single inner exception.
                                    exception = exception.InnerException;
                                    level = level + 1;
                                    continue;
                                }
                        }

                    break;
                }
        }
}