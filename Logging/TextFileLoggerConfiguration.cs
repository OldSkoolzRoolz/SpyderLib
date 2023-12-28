namespace KC.Apps.SpyderLib.Logging;

public class TextFileLoggerConfiguration
{
    #region Properteez

    public string EntryPrefix { get; set; }
    public string EntrySuffix { get; set; }
    public string LogLocation { get; set; }
    public string TimestampFormat { get; set; }
    public bool UseSingleLogFile { get; set; }
    public bool UseUtcTime { get; set; }

    #endregion
}