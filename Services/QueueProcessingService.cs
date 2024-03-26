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
    #region feeeldzzz

    private static int s_downloadAttempts;
    private readonly IMyClient _client;
    private readonly ILogger<QueueProcessingService> _logger;
    private readonly IBackgroundDownloadQue _taskQueue;
    private CancellationToken _stoppingToken;

    #endregion






    /// <inheritdoc />
    public QueueProcessingService(
        IBackgroundDownloadQue taskQueue,
        ILogger<QueueProcessingService> logger,
        IMyClient client)
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

    private async Task DoQueProcessingAsync()
        {
            List<Task> tasks = new();
            SemaphoreSlim semi = new(6, 6);


            while (!_stoppingToken.IsCancellationRequested)
                {
                    while (await _taskQueue.OutputAvailableAsync().ConfigureAwait(false))
                        {
                            await semi.WaitAsync(_stoppingToken)
                                .ConfigureAwait(false);


                            if (_taskQueue.Block.TryReceive(out var item))
                                {
                                    tasks.Add(Task.Run(async () =>
                                        {
                                            try
                                                {
                                                    await DownloadWorkItemAsync(item, _stoppingToken);
                                                }
                                            catch (TaskCanceledException)
                                                {
                                                    _logger.SpyderInfoMessage(
                                                        "Task cancelled exception during download");
                                                }
                                            catch (HttpRequestException e)
                                                {
                                                    Console.WriteLine(e);
                                                }
                                            finally
                                                {
                                                    semi.Release();
                                                }
                                        }));
                                }
                        }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

            semi.Dispose();
        }






    /// <summary>
    ///     File Download wrapper that provides error handling to prevent a single download failure from crashing entire app.
    /// </summary>
    /// <param name="workItem"></param>
    /// <param name="token"></param>
    private async Task DownloadWorkItemAsync(DownloadItem workItem, CancellationToken token)
        {
            try
                {
                    await DownloadWorkItemCoreAsync(workItem, token).ConfigureAwait(false);
                }
            catch (ArgumentNullException arg)
                {
                    _logger.SpyderInfoMessage(arg.Message);
                }
            catch (HttpIOException ioe)
                {
                    _logger.SpyderInfoMessage(ioe.Message);
                }
            catch (IOException e)
                {
                    _logger.SpyderInfoMessage(e.Message);
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
            Guard.IsNotNull(workItem);
            Guard.IsNotNull(_logger);

            Interlocked.Increment(ref s_downloadAttempts);

            var randomFileName = Path.GetRandomFileName();
            var savePath = Path.Combine(workItem.SavePath, randomFileName + ".mp4");

            using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    using (var responseStream = await _client.GetFileStreamFromWebAsync(workItem.Link)
                               .ConfigureAwait(false))
                        {
                            await responseStream.CopyToAsync(fileStream, stoppingToken)
                                .ConfigureAwait(false);
                        }
                }

            _logger.SpyderDebug($"Downloaded {workItem.Link} to {savePath}");
            _logger.SpyderDebug($"Tasks remaining::  {_taskQueue.Count}");
        }






    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.SpyderTrace("Queue Processing Service loaded");
            _stoppingToken = stoppingToken;

            DownloadController.DownloadQueueLoadComplete += OnDownloadQueueLoadComplete;

            while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                }
        }






    private void OnDownloadQueueLoadComplete(object sender, EventArgs e)
        {
            DoQueProcessingAsync();
        }






    private static void Init()
        {
            QueueProcessorLoadComplete.TrySetResult(true);
        }

    #endregion
}