#region

using System.Threading.Channels;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Logging;
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





    public ValueTask QueueBackgroundWorkItemAsync(DownloadItem workItem)
        {
            Guard.IsNotNull(value: workItem);

            return _queue.Writer.WriteAsync(item: workItem);
        }

    #endregion

    #region Public Methods

    public BackgroundDownloadQue(
        ILogger<BackgroundDownloadQue> logger)
        {
            BoundedChannelOptions options = new(500)
                {
                    FullMode = BoundedChannelFullMode.Wait
                };

            _queue = Channel.CreateBounded<DownloadItem>(options: options);
            logger.SpyderInfoMessage(message: "Background download Que is loaded");
        }

    #endregion
}