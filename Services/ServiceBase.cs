#region

using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Services;

public class ServiceBase
{
    protected ServiceBase(
        ILoggerFactory factory,
        SpyderOptions options,
        IHostApplicationLifetime lifetime)
        {
            this.LoggerFactory = factory;
            this.Options = options;
            this.AppLifetime = lifetime;
        }





    protected ServiceBase() { }


    #region Properteez

    protected IHostApplicationLifetime AppLifetime { get; }
    protected ILoggerFactory LoggerFactory { get; }
    protected SpyderOptions Options { get; }

    #endregion
}