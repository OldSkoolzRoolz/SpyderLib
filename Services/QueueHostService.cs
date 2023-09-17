using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace KC.Apps.Modules;



public interface IQueueHostService
{
    Task? ExecuteTask { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    void Dispose();
}




public class QueueHostService : BackgroundService, IQueueHostService
{
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
                await Task.Delay(3000);
            }
        }





    public sealed class QueueProcessingService : BackgroundService
    {
        private readonly ILogger<QueueProcessingService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;





        public QueueProcessingService(
            IBackgroundTaskQueue taskQueue,
            ILogger<QueueProcessingService> logger)
            {
                (_taskQueue, _logger) = (taskQueue, logger);
            }





        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                _logger.LogInformation(
                    $"{nameof(QueueProcessingService)} is running.{Environment.NewLine}" +
                    $"{Environment.NewLine}Tap W to add a work item to the " +
                    $"background queue.{Environment.NewLine}");

                return ProcessTaskQueueAsync(stoppingToken);
            }





        private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var workItem =
                            await _taskQueue.DequeueAsync(stoppingToken);

                        await workItem(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Prevent throwing if stoppingToken was signaled
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing task work item.");
                    }
                }
            }





        public override async Task StopAsync(CancellationToken stoppingToken)
            {
                _logger.LogInformation(
                    $"{nameof(QueueProcessingService)} is stopping.");

                await base.StopAsync(stoppingToken);
            }
    }
}