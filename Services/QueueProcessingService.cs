using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

/// <inheritdoc />
public sealed class QueueProcessingService : BackgroundService
{
    private readonly MyClient _client;
    private readonly ILogger<QueueProcessingService> _logger;
    private readonly SemaphoreSlim _semi = new(6);
    private readonly IBackgroundDownloadQue _taskQueue;
    private static int s_downloadAttempts;






    /// <inheritdoc />
    public QueueProcessingService(
        IBackgroundDownloadQue taskQueue,
        ILogger<QueueProcessingService> logger,
        MyClient client)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _client = client;

            Init();
        }






    #region Properteez

    public static int DownloadAttempts => s_downloadAttempts;
    public static TaskCompletionSource<bool> QueueProcessorLoadComplete { get; set; } = new();

    #endregion






    #region Private Methods

    /// <summary>
    ///     File Download wrapper that provides error handling to prevent a single download failure from crashing entire app.
    /// </summary>
    /// <param name="workItem"></param>
    /// <param name="token"></param>
    private async Task DownloadWorkItemAsync(DownloadItem workItem, CancellationToken token)
        {
            try
                {
                    await DownloadWorkItemCoreAsync(workItem: workItem, stoppingToken: token).ConfigureAwait(false);
                }
            catch (ArgumentNullException arg)
                {
                    _logger.SpyderInfoMessage(message: arg.Message);
                }
            catch (HttpIOException ioe)
                {
                    _logger.SpyderInfoMessage(message: ioe.Message);
                }
            catch (IOException e)
                {
                    _logger.SpyderInfoMessage(message: e.Message);
                }
        }






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
    private async Task DownloadWorkItemCoreAsync([NotNull] DownloadItem workItem, CancellationToken stoppingToken)
        {
            Guard.IsNotNull(value: workItem);
            Guard.IsNotNull(value: _logger);

            Interlocked.Increment(location: ref s_downloadAttempts);

            var randomFileName = Path.GetRandomFileName();
            var savePath = Path.Combine(path1: workItem.SavePath, randomFileName + ".mp4");

            using (var fileStream = new FileStream(path: savePath, mode: FileMode.Create))
                {
                    using (var responseStream = await _client.GetFileStreamFromWebAsync(address: workItem.Link)
                               .ConfigureAwait(false))
                        {
                            await responseStream.CopyToAsync(destination: fileStream, cancellationToken: stoppingToken)
                                .ConfigureAwait(false);
                        }
                }

            _logger.SpyderDebug($"Downloaded {workItem.Link} to {savePath}");
            _logger.SpyderDebug($"Tasks remaining::  {_taskQueue.Count}");
        }






    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
                {
                    while (await _taskQueue.OutputAvailableAsync().ConfigureAwait(false))
                        {
                            while (_taskQueue.Block.TryReceive(out var item))
                                {
                                    try
                                        {
                                            await _semi.WaitAsync(cancellationToken: stoppingToken)
                                                .ConfigureAwait(false);

                                            await DownloadWorkItemAsync(workItem: item, token: stoppingToken)
                                                .ConfigureAwait(false);
                                        }
                                    catch (TaskCanceledException)
                                        {
                                            _logger.SpyderInfoMessage(
                                                message: "Task cancelled exception during download");
                                        }
                                    catch (HttpRequestException e)
                                        {
                                            Console.WriteLine(value: e);
                                        }
                                    finally
                                        {
                                            _semi.Release();
                                        }
                                }
                        }
                }
        }






    private static void Init()
        {
            QueueProcessorLoadComplete.TrySetResult(true);
        }

    #endregion
}