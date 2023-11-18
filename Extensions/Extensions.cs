#region

using System.Net.Mime;

using JetBrains.Annotations;

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
            if (config is null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

            builder.Services.AddOptions<TextFileLoggerConfiguration>().Configure(logConfig =>
                logConfig.SetProperties<TextFileLoggerConfiguration>(config));
            builder.AddProvider(new TextFileLoggerProvider(config));
            builder.SetMinimumLevel(s_spyderOptions.LoggingLevel);

            builder.AddConsole().AddCustomFormatter(
                options =>
                    {
                        options.CustomPrefix = "~~<{ ";
                        options.CustomSuffix = " }>~~";
                    });
        }





    public static IServiceCollection AddSpyderService(
        this IServiceCollection services,
        SpyderOptions spyderOptions)
        {
            s_spyderOptions = spyderOptions ?? throw new ArgumentNullException(nameof(spyderOptions));


            services.RegisterServicesAndConfigureOptions(spyderOptions);


            return services;
        }

    #endregion

    #region Private Methods

    private static void RegisterServicesAndConfigureOptions(
        this IServiceCollection services,
        SpyderOptions spyderOptions)
        {
            services.AddOptions<SpyderOptions>()
                .Configure(options => options.SetProperties<SpyderOptions>(spyderOptions));
            if (spyderOptions.DownloadTagSource)
                {
                    services.AddSingleton<IBackgroundDownloadQue, BackgroundDownloadQue>();
                    services.AddHostedService<QueueProcessingService>();
                }
          
                    services.AddSingleton<SpyderMetrics>();
            //  //  services.AddSingleton<ICrawlerQue, CrawlerQue>();
            services.AddSingleton<OutputControl>();
            services.AddSingleton<ISpyderWeb, SpyderWeb>();
            services.AddSingleton<ICacheIndexService, CacheIndexService>();
            services.AddSingleton<IWebCrawlerController, WebCrawlerController>();
            services.AddHostedService<SpyderControlService>();
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
                            prop.SetValue(options, prop.GetValue(sourceOptions));
                        }
                }
        }

    #endregion
}

public static class TaskExtensions
{
    #region Public Methods

    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay((int)timeout.TotalMilliseconds)).ConfigureAwait(false))
                {
                    return await task.ConfigureAwait(false); // Task completed within timeout
                }


            throw new TimeoutException(); // Task timed out
        }





    public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false))
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