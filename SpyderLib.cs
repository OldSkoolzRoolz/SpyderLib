using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Worker.App
{
    public class SpyderLib : BackgroundService
    {
        private readonly ILogger<SpyderLib> _logger;

        public SpyderLib(ILogger<SpyderLib> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
             _logger.LogInformation($"{nameof(SpyderLib)} started.");

            try
            {
                await DoWork(stoppingToken);
            }
            catch (TaskCanceledException) { }
            catch (System.Exception ex)
            {
                _logger.LogCritical(ex.ToString());
                await this.StopAsync(stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("SpyderLib is running at: {time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
		{
            _logger.LogInformation($"{nameof(Worker)} stopped.");
            await base.StopAsync(cancellationToken);
        }
    }
}
