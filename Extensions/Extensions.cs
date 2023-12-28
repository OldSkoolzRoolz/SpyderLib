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

using Splat;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;



namespace KC.Apps.SpyderLib;

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
            // ArgumentNullException.ThrowIfNull(argument: config);

            _ = builder.Services.AddOptions<TextFileLoggerConfiguration>().Configure(logConfig =>
                logConfig.SetProperties(sourceOptions: config));
            //_ = builder.AddProvider(new TextFileLoggerProvider(config));

            builder.AddDebug().AddCustomFormatter(
                options =>
                    {
                        options.CustomPrefix = "~~<{ ";
                        options.CustomSuffix = " }>~~";
                    });
            //  builder.SetMinimumLevel(level: s_spyderOptions.LoggingLevel);
            _ = builder.AddFilter(category: "TextFileLogger", level: s_spyderOptions.LoggingLevel);
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






    public static ILoggingBuilder AddTextFileLogger(
        this ILoggingBuilder builder)
        {
            Guard.IsNotNull(value: builder);
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, TextFileLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions
                <TextFileLoggerConfiguration, TextFileLoggerProvider>(services: builder.Services);

            return builder;
        }






    public static ILoggingBuilder AddTextFileLogger(
        this ILoggingBuilder builder,
        Action<TextFileLoggerConfiguration> configure)
        {
            builder.AddTextFileLogger();
            builder.Services.Configure(configureOptions: configure);

            return builder;
        }






    public static TService GetRequiredService<TService>(this IReadonlyDependencyResolver resolver)
        {
            var service = resolver.GetService<TService>();
            if (service is null) // Splat is not able to resolve type for us
                {
                    throw new InvalidOperationException(
                        $"Failed to resolve object of type {typeof(TService)}"); // throw error with detailed description
                }

            return service; // return instance if not null
        }






    /// <summary>
    ///     Extension Method for registering Spyder for use in an AvaloniaUI environment.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="resolver"></param>
    /// <returns></returns>
    public static IMutableDependencyResolver RegisterAvaloniaSpyder(
        IMutableDependencyResolver mutableResolver,
        IReadonlyDependencyResolver resolver)
        {
            /*


var opt = new TextFileLoggerConfiguration
    {
        EntryPrefix = "~~~<",
        EntrySuffix = ">~~~~",
        LogLocation = @"/Storage/Spyder/Logs",
        TimestampFormat = "hh:MM:ss",
        UseSingleLogFile = false,
        UseUtcTime = false
    };
var httpfact = Locator.Current.GetService<IHttpClientFactory>();

var ow = Options.Create(opt);
var so = Options.Create(GetDefaultOptions());

var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
//builder.SetMinimumLevel(s_spyderOptions.LoggingLevel));


//loggerFactory.AddProvider(new TextFileLoggerProvider(ow));
var logger = loggerFactory.CreateLogger("SpyderLib");


Locator.CurrentMutable.RegisterConstant(new BackgroundDownloadQue(loggerFactory.CreateLogger<BackgroundDownloadQue>()));

var bg = Locator.Current.GetService<BackgroundDownloadQue>();

//Locator.CurrentMutable.RegisterConstant(new SpyderClient(),httpfact));

ISpyderClient httpclient = Locator.Current.GetService<ISpyderClient>();

Locator.CurrentMutable.RegisterConstant(() =>
    new QueueProcessingService(bg, loggerFactory.CreateLogger<QueueProcessingService>(), httpclient), typeof(QueueProcessingService));


var applifeTime = Locator.Current.GetService<IHostApplicationLifetime>();


Locator.CurrentMutable.RegisterConstant(() => new BackgroundDownloadQue(loggerFactory.CreateLogger<BackgroundDownloadQue>()), typeof(IBackgroundDownloadQue));

Locator.CurrentMutable.RegisterConstant(() => new OutputControl(), typeof(OutputControl));
Locator.CurrentMutable.RegisterConstant(() => new SpyderControlService(loggerFactory, so));
Locator.CurrentMutable.RegisterConstant(() => new CacheIndexService(null, loggerFactory.CreateLogger<CacheIndexService>(), so, httpclient),
    typeof(ICacheIndexService));

var cache = Locator.Current.GetService<CacheIndexService>();

Locator.CurrentMutable.RegisterConstant(() => new WebCrawlerController(cache),
    typeof(IWebCrawlerController));

Locator.CurrentMutable.RegisterLazySingleton(() => new DownloadController(so, loggerFactory.CreateLogger<DownloadController>(), bg, cache),
    typeof(IDownloadControl));

*/

            return mutableResolver;
        }






    /// <summary>
    ///     Extension Method for registering an instance of SpyderOptions for use in an AvaloniaUI environment.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="spyderOptions"></param>
    /// <returns></returns>
    public static IMutableDependencyResolver RegisterSpyderOptions(
        this IMutableDependencyResolver services,
        SpyderOptions spyderOptions)
        {
            services.RegisterConstant(value: spyderOptions);
            return services;
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
                .Configure(options => options.SetProperties(sourceOptions: spyderOptions));


            _ = services.AddSingleton<IBackgroundDownloadQue, BackgroundDownloadQue>();
            _ = services.AddSingleton<DownloadController>();
            _ = services.AddHostedService<QueueProcessingService>();

            _ = services.AddSingleton<SpyderMetrics>();
            _ = services.AddHttpClient(name: "SpyderClient");
            _ = services.AddSingleton<OutputControl>();
            _ = services.AddSingleton<ICacheIndexService, CacheIndexService>();
            _ = services.AddSingleton<IWebCrawlerController, WebCrawlerController>();
            _ = services.AddHostedService<SpyderControlService>();
            var prov = services.BuildServiceProvider();
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
} // SpyderLibExtensions#



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