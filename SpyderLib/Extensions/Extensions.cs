#region

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Extensions;

/// <summary>
/// </summary>
public static class SpyderLibExtensions
{
    #region Feeelldzz

    private static SpyderOptions _spyderOptions;

    #endregion

    #region Public Methods

    public static ILoggingBuilder AddSpyderLogging(
        this ILoggingBuilder        builder,
        TextFileLoggerConfiguration config)
        {
            builder.AddProvider(new TextFileLoggerProvider(config));
            builder.SetMinimumLevel(_spyderOptions.LoggingLevel);

            builder.AddConsole().AddCustomFormatter(
                                                    options =>
                                                        {
                                                            options.CustomPrefix = "~~<{ ";
                                                            options.CustomSuffix = " }>~~";
                                                        });


            return builder;
        }





    public static IServiceCollection AddSpyderService(
        this IServiceCollection services,
        SpyderOptions           spyderOptions)
        {
            _spyderOptions = spyderOptions ?? throw new ArgumentNullException(nameof(spyderOptions));
            if (!ValidateSpyderOptions(spyderOptions))
                {
                    Environment.Exit(99);
                }

            services.RegisterServicesAndConfigureOptions(spyderOptions);


            return services;
        }

    #endregion

    #region Private Methods

    private static bool ValidateSpyderOptions(
        SpyderOptions options)
        {
            var validOptions = true;

            if (options.UseLocalCache && !Directory.Exists(options.CacheLocation))
                {
                    try
                        {
                            var info = Directory.CreateDirectory(options.CacheLocation);
                            if (!info.Exists)
                                {
                                    Console.WriteLine("Configuration Error, cache location is not valid aborting.");
                                    validOptions = false;
                                }
                        }
                    catch (Exception)
                        {
                            Console.WriteLine("Configuration Error, cache location is not valid aborting.");
                            validOptions = false;
                        }
                }

            if (!Directory.Exists(options.LogPath))
                {
                    try
                        {
                            var info = Directory.CreateDirectory(options.LogPath);
                            if (!info.Exists)
                                {
                                    Console.WriteLine("Configuration Error, log path  is not valid aborting.");
                                    validOptions = false;
                                }
                        }
                    catch (Exception)
                        {
                            Console.WriteLine("Configuration Error, log path is not valid aborting.");
                            validOptions = false;
                        }
                }


            if (!Directory.Exists(options.OutputFilePath))
                {
                    try
                        {
                            var info = Directory.CreateDirectory(options.OutputFilePath);
                            if (!info.Exists)
                                {
                                    Console.WriteLine("Configuration Error, Output path  is not valid aborting.");
                                    validOptions = false;
                                }
                        }
                    catch (Exception)
                        {
                            Console.WriteLine("Configuration Error, output path is not valid aborting.");
                            validOptions = false;
                        }
                }


            if (string.IsNullOrWhiteSpace(options.StartingUrl))
                {
                    try
                        {
                            Console.WriteLine("Configuration Error,Starting url is invalid. aborting.");
                            validOptions = false;
                        }
                    catch (Exception)
                        {
                            Console.WriteLine("Configuration Error,Starting url is invalid. aborting.");
                            validOptions = false;
                        }
                }


            return validOptions;
        }





    private static void RegisterServicesAndConfigureOptions(
        this IServiceCollection services,
        SpyderOptions           spyderOptions)
        {
            services.AddOptions<SpyderOptions>().Configure(options => options.SetProperties(spyderOptions));
            if (spyderOptions.DownloadTagSource)
                {
            services.AddSingleton<IBackgroundDownloadQue, BackgroundDownloadQue>();
                }


//            services.AddHostedService<QueueProcessingService>();
            services.AddSingleton<ISpyderWeb, SpyderWeb>();
            services.AddSingleton<ICacheIndexService, CacheIndexService>();
            services.AddSingleton<IWebCrawlerController, WebCrawlerController>();
            services.AddHostedService<SpyderControlService>();
        }





    private static void SetProperties(
        this SpyderOptions options,
        SpyderOptions      spyderOptions)
        {
            var type = typeof(SpyderOptions);
            foreach (var prop in type.GetProperties())
                {
                    if (prop.CanRead && prop.CanWrite)
                        {
                            prop.SetValue(options, prop.GetValue(spyderOptions));
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
            if (task == await Task.WhenAny(task, Task.Delay((int)timeout.TotalMilliseconds)))
                {
                    return await task; // Task completed within timeout
                }


            throw new TimeoutException(); // Task timed out
        }





    public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
                {
                    await task;
                }
            else
                {
                    throw new TimeoutException();
                }
        }

    #endregion
}