using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;



namespace KC.Apps.SpyderLib.Extensions;

public static class SpyderLibExtensions
{
    #region feeeldzzz

    private static SpyderOptions s_spyderOptions;

    #endregion






    #region Public Methods

    public static void AddSpyderLogging(
        this ILoggingBuilder builder,
        TextFileLoggerConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(config);

        _ = builder.Services.AddOptions<TextFileLoggerConfiguration>().Configure(logConfig =>
            logConfig.SetProperties(config));

        builder.AddDebug().AddCustomFormatter(
            options =>
                {
                    options.CustomPrefix = "~~<{ ";
                    options.CustomSuffix = " }>~~";
                });
        builder.SetMinimumLevel(s_spyderOptions.LoggingLevel);
        _ = builder.AddFilter("TextFileLogger", s_spyderOptions.LoggingLevel);
        _ = builder.AddFilter("Microsoft", LogLevel.Information);
        _ = builder.AddFilter("System.Net.Http", LogLevel.Warning);
    }






    public static IServiceCollection AddSpyderService(
        this IServiceCollection services,
        SpyderOptions spyderOptions)
    {
        s_spyderOptions = spyderOptions ?? throw new ArgumentNullException(nameof(spyderOptions));


        services.RegisterServicesAndConfigureOptions(spyderOptions);


        return services;
    }






    public static ILoggingBuilder AddTextFileLogger(
        this ILoggingBuilder builder)
    {
        Guard.IsNotNull(builder);
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






    #region Private Methods

    private static SpyderOptions GetDefaultOptions()
    {
        return new()
        {
            CacheLocation = null,
            CapturedExternalLinksFilename = null,
            CapturedSeedUrlsFilename = null,
            ConcurrentCrawlingTasks = 0,
            CrawlInputFile = false,
            DownloadTagSource = false,
            EnableTagSearch = false,
            FollowExternalLinks = false,
            HtmlTagToSearchFor = null,
            InputFileName = null,
            KeepBaseLinks = false,
            KeepExternalLinks = false,
            LinkDepthLimit = 0,
            LinkPatternExclusions = Array.Empty<string>(),
            LoggingLevel = LogLevel.Trace,
            LogPath = null,
            OutputFileName = null,
            OutputFilePath = null,
            QueueCapacity = 0,
            StartingUrl = null,
            UseLocalCache = false,
            UseMetrics = false
        };
    }






    private static void RegisterServicesAndConfigureOptions(
        this IServiceCollection services,
        SpyderOptions spyderOptions)
    {
        _ = services.AddOptions<SpyderOptions>()
            .Configure(options => options.SetProperties(spyderOptions));


        _ = services.AddSingleton<IBackgroundDownloadQue, BackgroundDownloadQue>();
        _ = services.AddSingleton<DownloadController>();
        _ = services.AddHostedService<QueueProcessingService>();

        _ = services.AddSingleton<SpyderMetrics>();
        _ = services.AddHttpClient("SpyderClient");
        _ = services.AddSingleton<OutputControl>();
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
                prop.SetValue(options, prop.GetValue(sourceOptions));
            }
        }
    }

    #endregion
} // SpyderLibExtensions#



internal static class TaskExtensions
{
    #region Public Methods

    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        if (task ==
            await Task.WhenAny(task, Task.Delay((int)timeout.TotalMilliseconds))
                .ConfigureAwait(false))
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