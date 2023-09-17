#region

// ReSharper disable All
using System.Runtime.Caching;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;

using KC.Apps.Control;
using KC.Apps.Interfaces;
using KC.Apps.Logging;
using KC.Apps.Modules;
using KC.Apps.Properties;
using KC.Apps.Spyderlib.Services;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

#endregion

namespace SpyderLib;



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
                .Configure(
                    options =>
                    {
                        options.ScrapeDepthLevel = 3;
                        options.UseLocalCache = true;
                    });

            return services;
        }





    /// <summary>
    ///     Service extension method for adding the SpyderControl module as a service. Spyder options are
    ///     read from a config file in the ServiceCollection container.
    /// </summary>
    /// <param name="services">Adds service to the calling Service Collection</param>
    /// <param name="configSectionPath">Section header in appsettings.json file to load options from</param>
    /// <returns></returns>
    public static IServiceCollection AddSpyderService(this IServiceCollection services, string configSectionPath)
        {
            services.AddOptions<SpyderOptions>()
                .BindConfiguration(configSectionPath)
                .ValidateDataAnnotations();

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
        Action<SpyderOptions> configureOptions)
        {
            services.Configure(configureOptions: configureOptions);

            // Register lib services here...
            // services.AddScoped<ILibraryService, DefaultLibraryService>();
            return services;
        }





    /// <summary>
    ///     Use of this extension method you need to provide
    ///     an inlined options instance. In this scenario, you need
    ///     to supply an instance of your options object, SpyderOptions.
    /// </summary>
    /// <param name="services">Calling IServiceCollection</param>
    /// <param name="userOptions">Instance of SpyderOptions</param>
    /// <returns>Service collection with Spyder services added to the container</returns>
    public static IServiceCollection AddSpyderService(
        this IServiceCollection services,
        SpyderOptions userOptions)
        {
            services.AddMemoryCache(
                config =>
                {
                    config.TrackStatistics = true;
                    config.TrackLinkedCacheEntries = true;
                });

            services.AddSingleton(typeof(CacheSignal<>));
            services.ConfigureOptions(userOptions: userOptions);
            services.AddHostedService<SpyderControlService>();
            services.AddSingleton<IndexCacheService>();
            services.AddHttpClient<IndexCacheService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundDownloadQue>();

            // services.AddSingleton<IQueueHostService, QueueHostService>();

            //  services.AddLogging(new FileLogger(Path.Combine(Environment.CurrentDirectory, FILENAME), "SpyderService"));        
            return services;
        }





    private static IServiceCollection ConfigureOptions(
        this IServiceCollection services,
        SpyderOptions userOptions)
        {
            services.AddOptions<SpyderOptions>()
                .Configure(options => options.SetProperties(userOptions: userOptions));

            return services;
        }





    private static void SetProperties(
        this SpyderOptions options,
        SpyderOptions userOptions)
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





    /// <summary>
    ///     Asynchronously gets the value associated with this key if it exists, or generates a new entry using the provided
    ///     key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get.</typeparam>
    /// <param name="cache">The <see cref="IMemoryCache" /> instance this method extends.</param>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">
    ///     The factory task that creates the value associated with this key if the key does not exist in the
    ///     cache.
    /// </param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public static async Task<TItem?> GetOrCreateAsync<TItem>(
        this ObjectCache cache, string key, Func<CacheItem, Task<TItem>> factory)
        {
            if (!cache.TryGetValue(key, out object? result))
            {
                CacheItem entry = new(key);
                result = await factory(entry).ConfigureAwait(false);
                entry.Value = result;
            }

            return (TItem?)result;
        }





    public static bool TryGetValue(this ObjectCache cache, string key, out object? result)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var item = cache.GetCacheItem(key as string);
            if (item is not null)
            {
                //Hit
                result = item.Value;
                return true;
            }

            result = null;
            // Miss
            return false;
        }
}