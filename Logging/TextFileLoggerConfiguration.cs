namespace KC.Apps.Logging ;

    public class TextFileLoggerConfiguration
    {
        public bool UseUtcTime { get; set; }
        public bool UseSingleLogFile { get; set; }
        public LogRotationPolicy LogRotationPolicy { get; set; }
        public string? TimestampFormat { get; set; }
        internal string? EntryPrefix { get; set; }
        internal string? EntrySuffix { get; set; }
        public bool IncludeScopes { get; set; }
    }




    public enum LogRotationPolicy
    {
        Hourly = 0
        ,
        Daily = 1
        ,
        Weekly = 2
    }