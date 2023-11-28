#region

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;



#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#endregion

namespace KC.Apps.SpyderLib.Extensions;

public static class SpyderLibExtensions
{
    private static SpyderOptions s_spyderOptions;

    #region Public Methods

    public static void AddSpyderLogging(
        this ILoggingBuilder builder,
        TextFileLoggerConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(argument: builder);

            ArgumentNullException.ThrowIfNull(argument: config);

            ArgumentNullException.ThrowIfNull(argument: config);


            _ = builder.Services.AddOptions<TextFileLoggerConfiguration>().Configure(logConfig =>
                logConfig.SetProperties(sourceOptions: config));
            _ = builder.AddProvider(new TextFileLoggerProvider(config: config));
            builder.AddDebug().AddCustomFormatter(
                options =>
                    {
                        options.CustomPrefix = "~~<{ ";
                        options.CustomSuffix = " }>~~";
                    });
            //  builder.SetMinimumLevel(level: s_spyderOptions.LoggingLevel);
            _ = builder.AddFilter(category: "SpyderDebug", level: s_spyderOptions.LoggingLevel);
            _ = builder.AddFilter(category: "Microsoft", level: LogLevel.Information);
            _ = builder.AddFilter(category: "System.Net.Http", level: LogLevel.Warning);
        }





    public static IServiceCollection AddSpyderService(
        this IServiceCollection services,
        SpyderOptions spyderOptions)
        {
            s_spyderOptions = spyderOptions ?? throw new ArgumentNullException(nameof(spyderOptions));


            services.RegisterServicesAndConfigureOptions(spyderOptions: spyderOptions);


            return services;
        }

    #endregion

    #region Private Methods

    private static void RegisterServicesAndConfigureOptions(
        this IServiceCollection services,
        SpyderOptions spyderOptions)
        {
            _ = services.AddOptions<SpyderOptions>()
                .Configure(options => options.SetProperties(sourceOptions: spyderOptions));
            if (spyderOptions.DownloadTagSource)
                {
                    _ = services.AddSingleton<IDownloadControl, DownloadController>();
                    _ = services.AddSingleton<IBackgroundDownloadQue, BackgroundDownloadQue>();
                    _ = services.AddHostedService<QueueProcessingService>();
                }

            _ = services.AddSingleton<SpyderMetrics>();
            _ = services.AddHttpClient(name: "SpyderClient");
            _ = services.AddSingleton<ISpyderClient, SpyderClient>();
            _ = services.AddSingleton<OutputControl>();
            _ = services.AddSingleton<ISpyderWeb, SpyderWeb>();
            _ = services.AddSingleton<ICacheIndexService, CacheIndexService>();
            _ = services.AddSingleton<IWebCrawlerController, WebCrawlerController>();
            _ = services.AddHostedService<SpyderControlService>();
        }





    private static void SetProperties<T>(
        this T options,
        T sourceOptions) where T : class
        {
            var type = typeof(T);
            foreach (var prop in type.GetProperties())
                {
                    if (prop.CanRead && prop.CanWrite)
                        {
                            prop.SetValue(obj: options, prop.GetValue(obj: sourceOptions));
                        }
                }
        }

    #endregion
}

internal static class TaskExtensions
{
    #region Public Methods

    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task ==
                await Task.WhenAny(task1: task, Task.Delay((int)timeout.TotalMilliseconds))
                    .ConfigureAwait(false))
                {
                    return await task.ConfigureAwait(false); // Task completed within timeout
                }


            throw new TimeoutException(); // Task timed out
        }





    public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task1: task, Task.Delay(delay: timeout)).ConfigureAwait(false))
                {
                    await task.ConfigureAwait(false);
                }
            else
                {
                    throw new TimeoutException();
                }
        }

    #endregion
}