using System.Globalization;

using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;



namespace KC.Apps.SpyderLib.Logging;


internal class LogFormatter : ConsoleFormatter
{
    private readonly string _dateFormat;
    private readonly string _timeFormat;






    public LogFormatter(LogFormatterOptions options) : base("SpyderFormatter")
        {
            Guard.IsNotNull(options);
            _timeFormat = options.TimestampFormat;
            _dateFormat = "mm/dd/yyyy";
        }






    #region Public Methods

    /// <summary>Writes the log message to the specified TextWriter.</summary>
    /// <remarks>
    ///     if the formatter wants to write colors to the Debug, it can do so by embedding ANSI color codes into the string
    /// </remarks>
    /// <param name="logEntry">The log entry.</param>
    /// <param name="scopeProvider">The provider of scope data.</param>
    /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
        {
            ArgumentNullException.ThrowIfNull(scopeProvider);
            ArgumentNullException.ThrowIfNull(textWriter);
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
            if (!string.IsNullOrEmpty(LogFormatterOptions.CustomPrefix))
                {
                    formattedMessage = LogFormatterOptions.CustomPrefix + formattedMessage;
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
    internal DateTime ParseDate(
        string dateStr)
        {
            return DateTime.ParseExact(dateStr, _dateFormat, CultureInfo.InvariantCulture);
        }






    /// <summary>
    ///     Utility function to convert a <c>DateTime</c> object into printable data format used by the Logger subsystem.
    /// </summary>
    /// <param name="date">The <c>DateTime</c> value to be printed.</param>
    /// <returns>Formatted string representation of the input data, in the printable format used by the Logger subsystem.</returns>
    internal string PrintDate(
        DateTime date)
        {
            return date.ToString(_dateFormat, CultureInfo.InvariantCulture);
        }






    /// <summary>
    ///     Utility function to convert a <c>DateTime</c> object into printable time format used by the Logger subsystem.
    /// </summary>
    /// <param name="date">The <c>DateTime</c> value to be printed.</param>
    /// <returns>Formatted string representation of the input data, in the printable format used by the Logger subsystem.</returns>
    private string PrintTime(
        DateTime date)
        {
            return date.ToString(_timeFormat, CultureInfo.InvariantCulture);
        }

    #endregion
}