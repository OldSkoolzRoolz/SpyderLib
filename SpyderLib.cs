using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Worker.App;



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
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.ToString());
                await StopAsync(stoppingToken);
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