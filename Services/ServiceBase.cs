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





    #region Properteez

    protected ILoggerFactory LoggerFactory { get; }
    protected SpyderOptions Options { get; }
    protected IHostApplicationLifetime AppLifetime { get; }

    #endregion

    #region Public Methods

    protected ServiceBase()
        {
        }

    #endregion
}