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
            this.LoggerFactory = factory;
            this.Options = options;
            this.AppLifetime = lifetime;
        }





    public ServiceBase()
        {
        }





    protected ILoggerFactory LoggerFactory { get; }

    protected SpyderOptions Options { get; }

    protected IHostApplicationLifetime AppLifetime { get; }
}