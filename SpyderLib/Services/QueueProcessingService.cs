#region

using KC.Apps.SpyderLib.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Services;

public sealed class QueueProcessingService : BackgroundService
{
    private readonly ILogger<QueueProcessingService> _logger;
    private readonly IBackgroundDownloadQue _taskQueue;





    public QueueProcessingService(
        IBackgroundDownloadQue          taskQueue,
        ILogger<QueueProcessingService> logger
    )
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }





    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
                {
                    try
                        {

                            if (_taskQueue.Count > 0)
                                {
                                    var workItem = await _taskQueue.DequeueAsync(stoppingToken)
                                                                   .ConfigureAwait(false);

                                    await DownloadworkItem(workItem, stoppingToken);
                                }

                            await Task.Delay(10_000);
                            _logger.LogInformation("Task Processing service is polling...");
                        }
                    catch (OperationCanceledException)
                        {
                            _logger.LogInformation("A task was cancelled during processing");
                        }
                    catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred executing task work item.");

                            // handle other exceptions as necessary
                        }
                }
        }





    private async Task DownloadworkItem(DownloadItem workItem, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var cli = new HttpClient();
            using (var fl = new FileStream(workItem.SavePath, FileMode.Create))
                {
                    var strm = await cli.GetStreamAsync(workItem.Link).ConfigureAwait(false);
                    await strm.CopyToAsync(fl).ConfigureAwait(false);
                }
        }





    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {



            return ProcessTaskQueueAsync(stoppingToken);
        }
}