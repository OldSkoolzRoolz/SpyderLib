#region

using KC.Apps.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Services;

public class ServiceBase
{
    protected ServiceBase(
        ILoggerFactory           factory,
        SpyderOptions            options,
        IHostApplicationLifetime lifetime)
        {
            this.LoggerFactory = factory;
            this.Options = options;
            this.AppLifetime = lifetime;
        }





    #region Properteez

    protected ILoggerFactory LoggerFactory { get; set; }

    protected SpyderOptions Options { get; set; }

    protected IHostApplicationLifetime AppLifetime { get; set; }

    #endregion

    #region Public Methods

    public ServiceBase()
        {
        }

    #endregion
}