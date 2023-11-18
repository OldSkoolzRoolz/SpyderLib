namespace KC.Apps.SpyderLib.Logging;

public class TextFileLoggerConfiguration
{
    #region Public Methods

    public string EntryPrefix { get; init; }
    public string EntrySuffix { get; init; }
    public string LogLocation { get; init; }
    public string TimestampFormat { get; init; }
    public bool UseSingleLogFile { get; init; }
    public bool UseUtcTime { get; init; }

    #endregion
}