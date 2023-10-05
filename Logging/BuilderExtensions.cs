#region

using KC.Apps.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

#endregion




namespace KC.Apps;




public static class BuilderExtensions
    {
        #region Methods

        public static ILoggingBuilder AddTextFileLogger(
            this ILoggingBuilder builder)
            {
                builder.AddConfiguration();
                builder.Services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<ILoggerProvider, TextFileLoggerProvider>());

                LoggerProviderOptions.RegisterProviderOptions
                    <TextFileLoggerConfiguration, TextFileLoggerProvider>(builder.Services);

                return builder;
            }





        public static ILoggingBuilder AddTextFileLogger(
            this ILoggingBuilder builder,
            Action<TextFileLoggerConfiguration> configure)
            {
                builder.AddTextFileLogger();
                builder.Services.Configure(configure);
                return builder;
            }

        #endregion
    }