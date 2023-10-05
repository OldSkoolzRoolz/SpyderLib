#region

#endregion




#region

using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#endregion




namespace KC.Apps.Logging;




/// <summary>
///     A logger that writes messages to a text file.
/// </summary>
internal class TextFileLogger : ILogger
    {
        #region Instance variables

        private readonly string _name;
        [ThreadStatic] private static StreamWriter? t_streamWriter;

        #endregion





        internal TextFileLogger(
            string name,
            TextFileFormatter formatter,
            TextFileLoggerConfiguration config)
            {
                _name = name;
                this.Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
                this.Config = config;
            }





        #region Prop

        private TextFileLoggerConfiguration Config { get; }


        private TextFileFormatter Formatter { get; }

        #endregion




        #region Methods

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return default!;
            }





        public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }





        /// <summary>
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        /// <typeparam name="TState"></typeparam>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    {
                        return;
                    }

                ArgumentNullException.ThrowIfNull(formatter);
                var logName = GetLogFileName();
                using (t_streamWriter = new StreamWriter(logName, true))
                    {
                        var logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
                        this.Formatter.Write(in logEntry, t_streamWriter);
                    }
            }

        #endregion




        #region Methods

        private string GetLogFileName()
            {
                var name = "";
                name = this.Config.UseSingleLogFile
                    ? "FileLogger-UnifiedLog.log"
                    :

                    //create separate Log file for each category 
                    $"FileLogger-{_name}.log";

                //return path and filename
                name = Path.Combine(SpyderControlService.CrawlerOptions.LogPath, name);
                return name;
            }

        #endregion
    }