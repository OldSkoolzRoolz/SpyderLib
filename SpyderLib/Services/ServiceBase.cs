#region

using KC.Apps.Properties;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Services;

public class ServiceBase
{
    public ServiceBase(ILoggerFactory factory, SpyderOptions options, IHostApplicationLifetime lifetime)
        {
            _factory = factory;
            _options = options;
            _appLifetime = lifetime;
        }





    public ServiceBase()
        {
        }





    protected static ILoggerFactory _factory = null!;
    private static SpyderOptions _options = null!;
    protected static IHostApplicationLifetime _appLifetime;

    protected ILoggerFactory LoggerFactory => _factory;
    protected SpyderOptions Options => _options;

    protected IHostApplicationLifetime AppLifetime => _appLifetime;
}