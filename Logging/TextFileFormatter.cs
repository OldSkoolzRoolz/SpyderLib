using System.Globalization;

using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;



namespace KC.Apps.SpyderLib.Logging;

internal sealed class TextFileFormatter
{
    #region feeeldzzz

    private readonly TextFileLoggerConfiguration _formatterOptions;

    #endregion






    public TextFileFormatter(
        IOptions<TextFileLoggerConfiguration> options)
        {
            _formatterOptions = options.Value;
        }






    public TextFileFormatter(
        TextFileLoggerConfiguration config)
        {
            _formatterOptions = config;
        }






    #region Public Methods

    /// <summary>Writes a formatted log message to the Text file.</summary>
    /// <remarks>
    ///     The file logger options demonstrates the ease at which you can add data to the log message
    ///     The use of scopes has not been implemented
    /// </remarks>
    /// <param name="logEntry">The log entry.</param>
    /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public void Write<TState>(
        in LogEntry<TState> logEntry,
        StreamWriter textWriter)
        {
            try
                {
                    var stamp = _formatterOptions.UseUtcTime
                        ? DateTimeOffset.UtcNow.ToString(_formatterOptions.TimestampFormat,
                            CultureInfo.CurrentCulture)
                        : DateTimeOffset.Now.ToString(_formatterOptions.TimestampFormat,
                            CultureInfo.CurrentCulture);

                    var message =
                        logEntry.Formatter(
                            logEntry.State, logEntry.Exception);

                    if (string.IsNullOrWhiteSpace(message))
                        {
                            return;
                        }


                    // Using the new interpolated string format we can simplify the method and easier to catch formatting issues
                    //  Is there any performance hits associated with this technique?
                    var formattedlog =
                        $$"""
                          {{_formatterOptions.EntryPrefix}} {{stamp}}: {{logEntry.Category}}[{{logEntry.EventId}}] {{logEntry
                              .LogLevel}}- {{message}} {{_formatterOptions.EntrySuffix}}
                          """;

                    // text log files typically are single line entries so multi-line is not an option in this logger
                    // only Writeline method is used.
                    textWriter.WriteLine(formattedlog);
                    textWriter.Flush();
                }
            catch (SpyderException)
                {
                    //Make sure there is a Debug
                }
        }

    #endregion
}