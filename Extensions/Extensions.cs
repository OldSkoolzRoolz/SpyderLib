#region

using Microsoft.Extensions.DependencyInjection;

#endregion

namespace KC.Apps.SpyderLib.Extensions;

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
        services.AddOptions<KC.Apps.SpyderLib.Properties.SpyderOptions>()
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
        this IServiceCollection                            services,
        Action<KC.Apps.SpyderLib.Properties.SpyderOptions> configureOptions)
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
        KC.Apps.SpyderLib.Properties.SpyderOptions                            userOptions)
    {
        services
            .ConfigureOptions(userOptions: userOptions)
            .AddHostedService<ISpyderControl>();

        return services;
    }





    private static IServiceCollection ConfigureOptions(this IServiceCollection services,
        KC.Apps.SpyderLib.Properties.SpyderOptions                             userOptions)
    {
        services.AddOptions<KC.Apps.SpyderLib.Properties.SpyderOptions>()
                .Configure(options => options.SetProperties(userOptions: userOptions));

        return services;
    }





    private static void SetProperties(this KC.Apps.SpyderLib.Properties.SpyderOptions options,
        KC.Apps.SpyderLib.Properties.SpyderOptions                                    userOptions)
    {
        var type = typeof(KC.Apps.SpyderLib.Properties.SpyderOptions);
        foreach (var prop in type.GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
            {
                prop.SetValue(obj: options, prop.GetValue(obj: userOptions));
            }
        }
    }
}