#region

using Microsoft.Extensions.Logging;

#endregion

namespace SpyderLib.Logging;

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
        _output = new(fileName: filePath);
    }





    #region Methods

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(output: _output, category: categoryName);
    }





    /// <inheritdoc />
    public void Dispose()
    {
        _output.Dispose();
    }

    #endregion
}

/// <summary>
///     Extension methods to configure ILoggingBuilder with FileLoggerProvider
/// </summary>
public static class FileLoggerProviderExtensions
{
    #region Methods

    /// <summary>
    ///     Add <see cref="FileLoggerProvider" /> to <paramref name="builder" />
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="filePathName">The log file path</param>
    /// <returns>The logging builder.</returns>
    public static ILoggingBuilder AddFile(
        this ILoggingBuilder builder,
        string               filePathName)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.AddProvider(new FileLoggerProvider(filePath: filePathName));
        return builder;
    }

    #endregion
}