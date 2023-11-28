#region

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Properties;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.SpyderLib.Modules;

public interface IModFactory
{
    #region Public Methods

    ISpyderWeb GetSpyderWeb();

    #endregion
}

public sealed class ModFactory
{
    #region Feeelldzz

    private static ICrawlerQue _crawlerQue;

    private static SpyderOptions _options;
    private static ILoggerFactory _logFactory;


    private static ModFactory _instance;


// Create an instance of the class with the Lazy<T> type for thread safety
    private static readonly Lazy<ModFactory> lazy = new(() => new ModFactory());

    #endregion

    #region Other Fields

    private readonly ICacheIndexService _cache;
    private readonly ISpyderWeb _spyderWeb;
    private IBackgroundDownloadQue _downloadQue;

    #endregion





    // Make the constructor private so that this class cannot be instantiated
    private ModFactory()
        {
        }





    #region Public Methods

    public ICacheIndexService GetCacheIndex()
        {
            return _cache;
        }





    public static ILogger GetConfiguredLogger(
        Type type)
        {
            return _logFactory.CreateLogger(nameof(type));
        }





    // This is the newly created function
    public ILogger<T> GetTypedLogger<T>()
        {
            return _logFactory.CreateLogger<T>();
        }





    public void Initialize(
        ILoggerFactory           factory,
        IOptions<SpyderOptions>  options,
        IHostApplicationLifetime appLifetime,
        IBackgroundDownloadQue   downloadQue,
        ICrawlerQue              crawlerQue)
        {
            _logFactory = factory;
            _options = options.Value;
            _downloadQue = downloadQue;


        }





    // Return the instance
    public static ModFactory Instance => lazy.Value;

    #endregion
}