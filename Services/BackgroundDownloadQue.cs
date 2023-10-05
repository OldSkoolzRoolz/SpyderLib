#region

using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

#endregion




namespace KC.Apps.SpyderLib.Modules;




public interface IBackgroundTaskQueue
    {
        #region Methods

        ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken);





        ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem);

        #endregion
    }




/// <summary>
///     Download que
/// </summary>
public sealed class BackgroundDownloadQue : IBackgroundTaskQueue
    {
        #region Instance variables

        private readonly ILogger<BackgroundDownloadQue> _logger;


        private Channel<Func<CancellationToken, ValueTask>> _queue;

        #endregion





        public BackgroundDownloadQue(ILogger<BackgroundDownloadQue> logger)
            {
                _logger = logger;
                SetupQue(150);
            }





        #region Methods

        public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken)
            {
                var workItem =
                    await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                return workItem;
            }





        public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
            {
                if (workItem is null)
                    {
                        throw new ArgumentNullException(nameof(workItem));
                    }

                await _queue.Writer.WriteAsync(workItem).ConfigureAwait(false);
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

        #endregion
    }