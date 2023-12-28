using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Logging;

public static partial class LoggingMessages
{
    #region Public Methods

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Critical,
        Message = "{message}")]
    public static partial void CriticalOptions(this ILogger logger, string message);






    [LoggerMessage(EventId = 9999, Level = LogLevel.Debug, Message = "Debug Message == {message}")]
    public static partial void DebugTestingMessage(
        this ILogger logger,
        string message);






    [LoggerMessage(EventId = 475,
        Level = LogLevel.Information,
        Message = "Internal Crawler error, continuing...  {message}")]
    public static partial void GeneralCrawlerError(
        this ILogger logger,
        string message,
        Exception exception);






    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Error,
        Message = "An internal error occured:  {message}")]
    public static partial void InternalSpyderError(
        this ILogger logger,
        string message);






    [LoggerMessage(925, level: LogLevel.Error, message: "Http Exception thrown getting page {address}.. == {message}")]
    public static partial void LogHttpException(
        this ILogger logger,
        string message,
        string address);






    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Error,
        Message = "PageCache:: An error occured:  {message}")]
    public static partial void PageCacheException(
        this ILogger logger,
        string message);






    [LoggerMessage(
        EventId = 600,
        Level = LogLevel.Error,
        Message = "SpyderControl:: An error occured:  {message}")]
    public static partial void SpyderControlException(
        this ILogger logger,
        string message);






    [LoggerMessage(EventId = 30,
        Level = LogLevel.Debug,
        Message = "{message}")]
    public static partial void SpyderDebug(this ILogger logger, string message);






    [LoggerMessage(
        EventId = 900,
        Level = LogLevel.Error,
        Message = "SpyderDownloader:: An error occured:  {message}")]
    public static partial void SpyderDownloaderException(
        this ILogger logger,
        string message);






    [LoggerMessage(EventId = 40,
        Level = LogLevel.Error,
        Message = "{message}")]
    public static partial void SpyderError(this ILogger logger, string message);






    [LoggerMessage(
        EventId = 700,
        Level = LogLevel.Error,
        Message = "SpyderHelpers:: An error occured:  {message}")]
    public static partial void SpyderHelpersException(
        ILogger logger,
        string message);






    [LoggerMessage(EventId = 9977,
        Level = LogLevel.Information,
        Message = "{message}")]
    public static partial void SpyderInfoMessage(
        this ILogger logger,
        string message);






    [LoggerMessage(EventId = 20,
        Level = LogLevel.Trace,
        Message = "{message}")]
    public static partial void SpyderTrace(this ILogger logger, string message);






    [LoggerMessage(EventId = 50,
        Level = LogLevel.Warning,
        Message = "{message}")]
    public static partial void SpyderWarning(this ILogger logger, string message);






    [LoggerMessage(
        EventId = 400,
        Level = LogLevel.Error,
        Message = "SpyderWeb:: An error occured:  {message}")]
    public static partial void SpyderWebException(
        this ILogger logger,
        string message);






    [LoggerMessage(
        EventId = 969,
        Level = LogLevel.Error,
        Message = "Unknown library error. Unexpected results.  {message}")]
    public static partial void UnexpectedResultsException(this ILogger logger, string message);

    #endregion
}