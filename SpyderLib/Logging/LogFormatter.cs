#region

using System.Collections.Concurrent;
using System.Globalization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

#endregion


namespace KC.Apps.Logging;

public class LogFormatter : ConsoleFormatter
{
    #region Feeelldzz

    private const string
        DATE_FORMAT =
            "yyyy-MM-dd " +
            TIME_FORMAT; // Example: 2010-09-02 09:50:43.341 GMT - Variant of UniversalSorta­bleDateTimePat­tern

    internal const int MAX_LOG_MESSAGE_SIZE = 20000;
    private const string TIME_FORMAT = "HH:mm:ss.fff 'GMT'"; // Example: 09:50:43.341 GMT
    private static readonly ConcurrentDictionary<Type, Func<Exception, string>> _sExceptionDecoders = new();

    #endregion

    #region Other Fields

    private readonly LogFormatterOptions _formatterOptions;

    #endregion

    #region Public Methods

    public LogFormatter(
        LogFormatterOptions options) : base("SpyderFormatter")
        {
            _formatterOptions = options;
        }





    /// <summary>Writes the log message to the specified TextWriter.</summary>
    /// <remarks>
    ///     if the formatter wants to write colors to the console, it can do so by embedding ANSI color codes into the string
    /// </remarks>
    /// <param name="logEntry">The log entry.</param>
    /// <param name="scopeProvider">The provider of scope data.</param>
    /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public override void Write<TState>(
        in LogEntry<TState>    logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter             textWriter)
        {
            var logLevel = logEntry.LogLevel;
            var logMessage = logEntry.Formatter(logEntry.State, logEntry.Exception);

            // Format the log message
            var formattedMessage = $"{PrintTime(DateTime.Now)} {logLevel}: {logMessage}";


            // If exception exists, include it in the message 
            if (logEntry.Exception != null)
                {
                    formattedMessage += Environment.NewLine + logEntry.Exception;
                }

            // Prepend custom prefix from formatter options, if it exists
            if (!string.IsNullOrEmpty(_formatterOptions.CustomPrefix))
                {
                    formattedMessage = _formatterOptions.CustomPrefix + formattedMessage;
                }

            // Write the log message to the provided TextWriter
            textWriter.WriteLine(formattedMessage);
        }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Parses a date.
    /// </summary>
    /// <param name="dateStr">The date string.</param>
    /// <returns>The parsed date.</returns>
    internal static DateTime ParseDate(
        string dateStr)
        {
            return DateTime.ParseExact(dateStr, DATE_FORMAT, CultureInfo.InvariantCulture);
        }





    /// <summary>
    ///     Utility function to convert a <c>DateTime</c> object into printable data format used by the Logger subsystem.
    /// </summary>
    /// <param name="date">The <c>DateTime</c> value to be printed.</param>
    /// <returns>Formatted string representation of the input data, in the printable format used by the Logger subsystem.</returns>
    internal static string PrintDate(
        DateTime date)
        {
            return date.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
        }





    /// <summary>
    ///     Utility function to convert a <c>DateTime</c> object into printable time format used by the Logger subsystem.
    /// </summary>
    /// <param name="date">The <c>DateTime</c> value to be printed.</param>
    /// <returns>Formatted string representation of the input data, in the printable format used by the Logger subsystem.</returns>
    internal static string PrintTime(
        DateTime date)
        {
            return date.ToString(TIME_FORMAT, CultureInfo.InvariantCulture);
        }





    /// <summary>
    ///     Configures the exception decoder for the specified exception type.
    /// </summary>
    /// <param name="exceptionType">The exception type to configure a decoder for.</param>
    /// <param name="decoder">The decoder.</param>
    internal static void SetExceptionDecoder(
        Type                    exceptionType,
        Func<Exception, string> decoder)
        {
            _sExceptionDecoders.TryAdd(exceptionType, decoder);
        }

    #endregion
}