namespace KC.Apps.Logging;

public class TextFileLoggerConfiguration
{
    #region Public Methods

    public string EntryPrefix { get; set; }
    public string EntrySuffix { get; set; }
    public bool IncludeScopes { get; set; }
    public string LogLocation { get; set; }
    public string TimestampFormat { get; set; }
    public bool UseSingleLogFile { get; set; }
    public bool UseUtcTime { get; set; }

    #endregion
}

public enum LogRotationPolicy
{
    Hourly = 0,
    Daily = 1,
    Weekly = 2
}