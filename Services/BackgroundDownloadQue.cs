#region

using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

#endregion

//ReSharper disable All
namespace KC.Apps.Modules;



public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(
        Func<CancellationToken, ValueTask> workItem);





    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken);
}




/// <summary>
///     Download que
/// </summary>
public sealed class BackgroundDownloadQue : IBackgroundTaskQueue
{
    private readonly ILogger<BackgroundDownloadQue> _logger;


    private Channel<Func<CancellationToken, ValueTask>> _queue;





    public BackgroundDownloadQue(ILogger<BackgroundDownloadQue> logger)
        {
            _logger = logger;
            SetupQue(150);
        }





    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }





    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken)
        {
            Func<CancellationToken, ValueTask>? workItem =
                await _queue.Reader.ReadAsync(cancellationToken);

            return workItem;
        }





    [MemberNotNull("_queue")]
    public void SetupQue(int capacity)
        {
            BoundedChannelOptions options = new(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };

            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        }
}