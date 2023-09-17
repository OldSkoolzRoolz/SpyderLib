#region

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.Logging;



/// <summary>
///     <see cref="ILoggerProvider" /> which outputs to a log file.
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggingOutput _output;





    /// <summary>
    ///     Initializes a new instance of the <see cref="FileLoggerProvider" /> class.
    /// </summary>
    /// <param name="filePath">The log file path.</param>
    public FileLoggerProvider(string filePath)
        {
            _output = new FileLoggingOutput(filePath);
        }





    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_output, categoryName);
        }





    /// <inheritdoc />
    public void Dispose()
        {
            _output.Dispose();
        }
}




/// <summary>
///     Extension methods to configure ILoggingBuilder with FileLoggerProvider
/// </summary>
public static class FileLoggerProviderExtensions
{
    /// <summary>
    ///     Add <see cref="FileLoggerProvider" /> to <paramref name="builder" />
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="filePathName">The log file path</param>
    /// <returns>The logging builder.</returns>
    public static ILoggingBuilder AddFile(
        this ILoggingBuilder builder,
        string filePathName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddProvider(new FileLoggerProvider(filePathName));
            return builder;
        }
}