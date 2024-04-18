using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public interface IBackgroundDownloadQue
{
    BufferBlock<DownloadItem> Block { get; }
    Task Completion { get; }
    int Count { get; }


    Task Complete();


    ValueTask<DownloadItem> DequeueAsync(CancellationToken cancellationToken);


    Task<bool> OutputAvailableAsync();


    void PostingComplete();


    Task QueueBackgroundWorkItemAsync(DownloadItem workItem);


    Task<DownloadItem> ReceiveAsync();
}



/// <summary>
///     Background Download Que for QueItems using a BufferBlock
/// </summary>
public class BackgroundDownloadQue : IBackgroundDownloadQue
{
    #region feeeldzzz

    private readonly Channel<DownloadItem> _queue;

    #endregion






    /// <summary>
    ///     Constructor for the Background Download Que.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ILogger{TCategoryName}" /> used for logging.
    /// </param>
    public BackgroundDownloadQue(
        ILogger<BackgroundDownloadQue> logger)
        {
            // Setup the Options for the Queue
            var options = new BoundedChannelOptions(500)
                {
                    // Set the FullMode to Wait so that when the Queue is full
                    // the code will wait until there is space in the queue.
                    FullMode = BoundedChannelFullMode.Wait
                };

            // Create the Channel with the options
            _queue = Channel.CreateBounded<DownloadItem>(options);

            // Create the BufferBlock for the Queue
            this.Block = new();

        

            // Set the Download Queue Load Complete Task to be Complete
            _ = DownloadQueLoadComplete.TrySetResult(true);
        }






    #region Properteez

    public BufferBlock<DownloadItem> Block { get; }
    public Task Completion => this.Block.Completion;
    public int Count => this.Block.Count;
    public static TaskCompletionSource<bool> DownloadQueLoadComplete { get; set; } = new();

    #endregion






    #region Public Methods

    public Task Complete()
        {
            this.Block.Complete();
            return Task.CompletedTask;
        }






    public async ValueTask<DownloadItem> DequeueAsync(CancellationToken cancellationToken)
        {
            try
                {
                    return await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
            catch (OperationCanceledException)
                {
                    return default;
                }
        }






    public Task<bool> OutputAvailableAsync()
        {
            return this.Block.OutputAvailableAsync();
        }






    public void PostingComplete()
        {
            this.Block.Complete();
        }






    public async Task QueueBackgroundWorkItemAsync(DownloadItem workItem)
        {
            Guard.IsNotNull(workItem);
            try
                {
                    _ = await this.Block.SendAsync(workItem).ConfigureAwait(false);
                }
            catch (Exception e)
                {
                    Console.WriteLine(Resources1.Buffer_Block_Data_Error);
                }
        }






    public Task<DownloadItem> ReceiveAsync()
        {
            return this.Block.ReceiveAsync(TimeSpan.FromSeconds(30));
        }

    #endregion
}