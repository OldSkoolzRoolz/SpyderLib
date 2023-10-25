#region

using System.Threading.Channels;
using KC.Apps.SpyderLib.Models;
using Microsoft.Extensions.Logging;

#endregion


namespace KC.Apps.SpyderLib.Services;

public interface IBackgroundDownloadQue
{
    int Count { get; }




    ValueTask QueueBackgroundWorkItemAsync(DownloadItem workItem);





    ValueTask<DownloadItem> DequeueAsync(
        CancellationToken cancellationToken);
}

/// <summary>
///     Download que
/// </summary>
public class BackgroundDownloadQue : IBackgroundDownloadQue
{
    private readonly Channel<DownloadItem> _queue;





    public BackgroundDownloadQue(ILogger<BackgroundDownloadQue> logger)
        {
            BoundedChannelOptions options = new(100)
                {
                    FullMode = BoundedChannelFullMode.Wait
                };

            _queue = Channel.CreateBounded<DownloadItem>(options);
            logger.LogInformation("Background download Que is loaded");
        }





    public int Count => _queue.Reader.Count;





    public async ValueTask QueueBackgroundWorkItemAsync(DownloadItem workItem)
        {
            if (workItem is null)
                {
                    throw new ArgumentNullException(nameof(workItem));
                }

            await _queue.Writer.WriteAsync(workItem).ConfigureAwait(false);
        }





    public async ValueTask<DownloadItem> DequeueAsync(
        CancellationToken cancellationToken)
        {
            var workItem =
                await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            return workItem;
        }
}