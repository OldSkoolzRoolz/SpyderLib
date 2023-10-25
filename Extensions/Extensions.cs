#region

using System.Runtime.Caching;
using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.Extensions;

/// <summary>
/// </summary>
public static class SpyderLibExtensions
{
    private static SpyderOptions _crawlerOptions;





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
        SpyderOptions           userOptions)
        {
            _crawlerOptions = userOptions;

            services.AddSingleton<MyHttpClient>();

            services.AddOptions<SpyderOptions>().Configure(options => options.SetProperties(userOptions));

            services.AddSingleton<CacheIndexService>();
            services.AddSingleton<IBackgroundDownloadQue, BackgroundDownloadQue>();
            services.AddHostedService<QueueProcessingService>();
            services.AddHostedService<SpyderControlService>();

            return services;
        }





    public static ILoggingBuilder AddSpyderLogging(
        this ILoggingBuilder builder, TextFileLoggerConfiguration config)
        {
            builder.AddConsole().AddCustomFormatter(
                                                    options =>
                                                        {
                                                            options.CustomPrefix = "~~<{ ";
                                                            options.CustomSuffix = " }>~~";
                                                        });

            builder.AddProvider(new TextFileLoggerProvider(config));
            builder.SetMinimumLevel(_crawlerOptions.LoggingLevel);

            return builder;
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
    public static async Task<TItem> GetOrCreateAsync<TItem>(
        this ObjectCache cache, string key, Func<CacheItem, Task<TItem>> factory)
        {
            if (!cache.TryGetValue(key, out var result))
                {
                    CacheItemPolicy polly = new();
                    CacheItem entry = new(key);
                    result = await factory(entry).ConfigureAwait(false);
                    entry.Value = result ?? string.Empty;
                    cache.Set(entry, polly);
                }

            return (TItem)result;
        }





    public static bool TryGetValue(this ObjectCache cache, string key, out object result)
        {
            if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

            var item = cache.GetCacheItem(key);
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





    private static void SetProperties(
        this SpyderOptions options,
        SpyderOptions      userOptions)
        {
            var type = typeof(SpyderOptions);
            foreach (var prop in type.GetProperties())
                {
                    if (prop.CanRead && prop.CanWrite)
                        {
                            prop.SetValue(options, prop.GetValue(userOptions));
                        }
                }
        }
}