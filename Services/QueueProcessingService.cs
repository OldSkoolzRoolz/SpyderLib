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

    #region Public Methods

    public QueueProcessingService(
        IBackgroundDownloadQue taskQueue,
        ILogger<QueueProcessingService> logger
    )
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

    #endregion

    #region Private Methods

    private async Task ProcessTaskQueueAsync(
        CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
                {
                    try
                        {
                            if (_taskQueue.Count > 0)
                                {
                                    var workItem = await _taskQueue.DequeueAsync(stoppingToken)
                                        .ConfigureAwait(false);

                                    await DownloadworkItem(workItem, stoppingToken).ConfigureAwait(false);
                                }

                            await Task.Delay(10_000, stoppingToken).ConfigureAwait(false);
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





    private async Task DownloadworkItem(
        DownloadItem workItem,
        CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(workItem.Link);
            _logger.LogDebug($"Attempting to download {workItem.Link}");
            var cli = new HttpClient();
            var path = Path.Combine(workItem.SavePath, Path.GetFileName(workItem.Link));
            await using var fl = new FileStream(path, FileMode.Create);
            var stream = await cli.GetStreamAsync(workItem.Link, stoppingToken).ConfigureAwait(false);
            await stream.CopyToAsync(fl, stoppingToken).ConfigureAwait(false);
        }





    protected override Task ExecuteAsync(
        CancellationToken stoppingToken)
        {
            return ProcessTaskQueueAsync(stoppingToken);
        }

    #endregion
}