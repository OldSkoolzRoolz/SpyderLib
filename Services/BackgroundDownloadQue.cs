using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

public interface IBackgroundDownloadQue
{
    #region Properteez

    BufferBlock<DownloadItem> Block { get; }
    Task Completion { get; }
    int Count { get; }

    #endregion






    #region Public Methods

    Task Complete();


    ValueTask<DownloadItem> DequeueAsync(CancellationToken cancellationToken);


    Task<bool> OutputAvailableAsync();


    void PostingComplete();


    Task QueueBackgroundWorkItemAsync(DownloadItem workItem);


    Task<DownloadItem> ReceiveAsync();

    #endregion
}



/// <summary>
///     Download que
/// </summary>
public class BackgroundDownloadQue : IBackgroundDownloadQue
{
    private readonly Channel<DownloadItem> _queue;






    public BackgroundDownloadQue(
        ILogger<BackgroundDownloadQue> logger)
        {
            BoundedChannelOptions options = new(500)
                {
                    FullMode = BoundedChannelFullMode.Wait
                };

            _queue = Channel.CreateBounded<DownloadItem>(options: options);

            this.Block = new();
            logger.SpyderInfoMessage(message: "Background download Que is loaded");

            DownloadQueLoadComplete.TrySetResult(true);
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
                    return await _queue.Reader.ReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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
            Guard.IsNotNull(value: workItem);

            await this.Block.SendAsync(item: workItem).ConfigureAwait(false);

            Console.WriteLine(value: Resources1.Buffer_Block_Data_Error);


            //            return _queue.Writer.WriteAsync(workItem);
        }






    public Task<DownloadItem> ReceiveAsync()
        {
            return this.Block.ReceiveAsync(TimeSpan.FromSeconds(30));
        }

    #endregion
}