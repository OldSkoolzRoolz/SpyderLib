using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public sealed class QueueProcessingService(
    IBackgroundDownloadQue taskQueue,
    ILogger<QueueProcessingService> logger,
    ISpyderClient client)
    : BackgroundService
{
    private const int NUMBER_OF_PARALLEL_TASKS = 4;

    #region Private Methods

    /// <summary>
    ///     Asynchronously downloads a work item provided.
    /// </summary>
    /// <param name="workItem">The item to be downloaded, containing link and save path information.</param>
    /// <param name="stoppingToken">A cancellation token that can be used to request the operation to be cancelled.</param>
    /// <exception cref="System.OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when the provided workItem link is null.</exception>
    /// <remarks>
    ///     This method creates an HttpClient instance and attempts to download the work item from the provided link.
    ///     It logs the download attempt, ensures the workItem link is not null, and supports cancellation.
    ///     It will write the downloaded data to a file stream created at the provided workItem's save path.
    /// </remarks>
    private async Task DownloadWorkItem([NotNull] DownloadItem workItem, CancellationToken stoppingToken)
        {
            Guard.IsNotNull(value: workItem);

            logger.SpyderDebug($"Starting to download {workItem.Link}");

            var randomFileName = Path.GetRandomFileName();
            var savePath = Path.Combine(path1: workItem.SavePath, randomFileName + ".mp4");

            var fileStream = new FileStream(path: savePath, mode: FileMode.Create);
            await using var _ = fileStream.ConfigureAwait(false);
            var responseStream = await client
                .GetStreamAsync(new(uriString: workItem.Link), cancellationToken: stoppingToken).ConfigureAwait(false);

            await responseStream.CopyToAsync(destination: fileStream, cancellationToken: stoppingToken)
                .ConfigureAwait(false);

            logger.SpyderDebug($"Downloaded {workItem.Link} to {savePath}");
        }





    protected override Task ExecuteAsync(
        CancellationToken stoppingToken)
        {
            return ProcessTaskQueueItemsAsync(token: stoppingToken);
        }





    private async Task ProcessTaskQueueItemsAsync(CancellationToken token)
        {
            var tasks = Enumerable.Range(0, count: NUMBER_OF_PARALLEL_TASKS).Select(_ => ProcessTasks(token: token))
                .ToArray();
            _ = await Task.WhenAny(tasks: tasks).ConfigureAwait(false);
        }





    private async Task ProcessTasks(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
                {
                    var taskToProcess = await taskQueue.DequeueAsync(cancellationToken: token).ConfigureAwait(false);

                    if (token.IsCancellationRequested)
                        break;

                    try
                        {
                            await DownloadWorkItem(workItem: taskToProcess, stoppingToken: token).ConfigureAwait(false);
                        }
                    catch (SpyderException)
                        {
                            logger.SpyderError($"Error occurred during downloading of {taskToProcess.Link}");
                        }
                    // Uncomment below line if you want to wait between dequeuing tasks.
                    // await Task.Delay(10_000, cancellationToken: token).ConfigureAwait(false);
                }
        }

    #endregion
}