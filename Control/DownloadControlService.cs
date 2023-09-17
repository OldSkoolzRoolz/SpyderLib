#region

// ReSharper disable All
using KC.Apps.Interfaces;
using KC.Apps.Logging;
using KC.Apps.Models;
using KC.Apps.Modules;
using KC.Apps.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PuppeteerSharp;

#endregion

namespace KC.Apps.Control
{
    public interface IDownloadControlService
    {
    }




    public class DownloadControlService : BackgroundService, IDownloadControlService
    {
        private readonly ILogger<DownloadControlService> _logger;
        private readonly IBackgroundTaskQueue _taskQue;





        public DownloadControlService(
            ILoggerFactory factory, IBackgroundTaskQueue taskQue,
            IOptions<SpyderOptions> options)
            {
                _logger = factory.CreateLogger<DownloadControlService>();
                _taskQue = taskQue;
            }





        public override async Task StopAsync(CancellationToken stoppingToken)
            {
                _logger.LogInformation(
                    $"{nameof(DownloadControlService)} is stopping.");

                await base.StopAsync(stoppingToken);
            }





        /// <summary>
        ///     This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The
        ///     implementation should return a task that represents
        ///     the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">
        ///     Triggered when
        ///     <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is
        ///     called.
        /// </param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(14000);
                    _logger.DebugTestingMessage("Polling Download Que");
                }
            }
    }
}