#region

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Logging;

public static partial class LoggingMessages
{
    #region Public Methods

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





    [LoggerMessage(9977, LogLevel.Information, "{message}")]
    public static partial void GeneralSpyderMessage(
        this ILogger logger,
        string message);





    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Error,
        Message = "An internal error occured:  {message}")]
    public static partial void InternalSpyderError(
        this ILogger logger,
        string message);





    [LoggerMessage(925, LogLevel.Error, "Http Exception thrown getting page {address}.. == {message}")]
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





    [LoggerMessage(
        EventId = 900,
        Level = LogLevel.Error,
        Message = "SpyderDownloader:: An error occured:  {message}")]
    public static partial void SpyderDownloaderException(
        this ILogger logger,
        string message);





    [LoggerMessage(
        EventId = 700,
        Level = LogLevel.Error,
        Message = "SpyderHelpers:: An error occured:  {message}")]
    public static partial void SpyderHelpersException(
        this ILogger logger,
        string message);





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
    public static partial void UnexpectedResultsException(
        this ILogger logger,
        string message);

    #endregion
}