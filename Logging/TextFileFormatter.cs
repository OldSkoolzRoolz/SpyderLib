#region

#endregion


#region

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.Logging;

/// <inheritdoc cref="ConsoleFormatter" />
internal class TextFileFormatter
{
    private readonly TextFileLoggerConfiguration _formatterOptions;





    public TextFileFormatter(IOptions<TextFileLoggerConfiguration> options)
        {
            _formatterOptions = options.Value;
        }





    public TextFileFormatter(TextFileLoggerConfiguration config)
        {
            _formatterOptions = config;
        }





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
        StreamWriter        textWriter)
        {
            string stamp;
            if (_formatterOptions.UseUtcTime)
                {
                    stamp = DateTimeOffset.UtcNow.ToString(_formatterOptions.TimestampFormat);
                }
            else
                {
                    stamp = DateTimeOffset.Now.ToString(_formatterOptions.TimestampFormat);
                }

            var message =
                logEntry.Formatter(
                                   logEntry.State, logEntry.Exception);

            if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

            try
                {
                    // Using the new interpolated string format we can simplify the method and easier to catch formatting issues
                    //  Is there any performance hits associated with this technique?
                    var formattedlog =
                        $$"""{{_formatterOptions.EntryPrefix}} {{stamp}}: {{logEntry.Category}}[{{logEntry.EventId}}] {{logEntry
                            .LogLevel}}- {{message}} {{_formatterOptions.EntrySuffix}} """;

                    // text log files typically are single line entries so multi-line is not an option in this logger
                    // only Writeline method is used.
                    textWriter.WriteLine(formattedlog);
                }
            catch (Exception e)
                {
                    //Make sure there is a console
                    Console.WriteLine(e);

                    // pointless to log an error about a broken logger with our broken logger...... Paradox? Conundrum? Migraine!
                }
        }





    /// <summary>Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.</summary>
}