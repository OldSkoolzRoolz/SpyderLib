#region

using System.Threading.Channels;

using KC.Apps.SpyderLib.Models;

using Microsoft.Extensions.Logging;

#endregion

namespace KC.Apps.SpyderLib.Services;

public interface IBackgroundDownloadQue
{
    #region Public Methods

    int Count { get; }





    ValueTask<DownloadItem> DequeueAsync(
        CancellationToken cancellationToken);





    ValueTask QueueBackgroundWorkItemAsync(
        DownloadItem workItem);

    #endregion
}

/// <summary>
///     Download que
/// </summary>
public class BackgroundDownloadQue : IBackgroundDownloadQue
{
    private readonly Channel<DownloadItem> _queue;

    #region Interface Members

    public int Count => _queue.Reader.Count;





    public ValueTask QueueBackgroundWorkItemAsync(
        DownloadItem workItem)
        {
            if (workItem is null)
                {
                    throw new ArgumentNullException(nameof(workItem));
                }

            return _queue.Writer.WriteAsync(workItem);
        }





    public async ValueTask<DownloadItem> DequeueAsync(
        CancellationToken cancellationToken)
        {
            var workItem =
                await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);


            return workItem;
        }

    #endregion

    #region Public Methods

    public BackgroundDownloadQue(
        ILogger<BackgroundDownloadQue> logger)
        {
            BoundedChannelOptions options = new(100)
                {
                    FullMode = BoundedChannelFullMode.Wait
                };

            _queue = Channel.CreateBounded<DownloadItem>(options);
            logger.LogInformation("Background download Que is loaded");
        }

    #endregion
}