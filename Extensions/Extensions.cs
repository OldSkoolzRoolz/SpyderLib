#region
// ReSharper disable All
using System.Runtime.InteropServices.ComTypes;

using KC.Apps.Interfaces;
using KC.Apps.Modules;
using KC.Apps.Properties;

using Microsoft.Extensions.DependencyInjection;

using PuppeteerSharp;

#endregion

namespace KC.Apps.Extensions;

/// <summary>
/// </summary>
public static class SpyderLibExtensions
{
    /// <summary>
    ///     parameterless Service registration method using default settings for Spyder Options
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddSpyderService(this IServiceCollection services)
    {
        services.AddOptions<SpyderOptions>()
                .Configure(options =>
                           {
                               options.ScrapeDepthLevel = 3;
                               options.UseLocalCache = true;
                           }).ValidateOnStart();

        return services;
    }





    /// <summary>
    ///     Allows SpyderOptions to be provided using lambda expression or object initializer
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddSpyderService(
        this IServiceCollection services,
        Action<SpyderOptions>   configureOptions)
    {
        services.Configure(configureOptions: configureOptions);

        // Register lib services here...
        // services.AddScoped<ILibraryService, DefaultLibraryService>();

        return services;
    }





    /// <summary>
    ///     Service registration for Spyder. Pass in SpyderOptions instance for options param
    /// </summary>
    /// <param name="services"></param>
    /// <param name="userOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddSpyderService(this IServiceCollection services,
        SpyderOptions                                                         userOptions)
    {
        services
            .ConfigureOptions(userOptions: userOptions)
            .AddHostedService<ISpyderControl>();

        services.AddSingleton <IBackgroundTaskQueue>(_ =>
                                                     {
                                                         var queCapcity = userOptions.QueueCapacity;

                                                         return new BackgroundDownloadQue(queCapcity);
                                                     });
        services.AddSingleton<QueueHostService>();
        return services;
    }





    private static IServiceCollection ConfigureOptions(this IServiceCollection services,
        SpyderOptions                                                          userOptions)
    {
        services.AddOptions<SpyderOptions>()
                .Configure(options => options.SetProperties(userOptions: userOptions));

        return services;
    }





    private static void SetProperties(this SpyderOptions options,
        SpyderOptions                                    userOptions)
    {
        var type = typeof(SpyderOptions);
        foreach (var prop in type.GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
            {
                prop.SetValue(obj: options, prop.GetValue(obj: userOptions));
            }
        }
    }
}