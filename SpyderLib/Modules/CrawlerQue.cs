#region

using System.Threading.Tasks.Dataflow;

#endregion


namespace KC.Apps.SpyderLib.Modules;

public interface ICrawlerQue
{
    #region Public Methods

    void AddItemToQueue(
        QueItem item);





    Task<QueItem> GetItemFromQueueAsync();




    bool IsQueueEmpty { get; }

    #endregion
}

/// <summary>
///     Crawler Que for QueItems using a BufferBlock
/// </summary>
public class CrawlerQue : ICrawlerQue
{
    #region Feeelldzz

// Create an instance of the class with the Lazy<T> type for thread safety
    private static readonly Lazy<CrawlerQue> _lazy = new(() => new CrawlerQue());

    #endregion

    #region Other Fields

    private readonly BufferBlock<QueItem> _queue = new();

    #endregion





    // Make the constructor private so that this class cannot be instantiated
    private CrawlerQue()
        {
        }





    #region Interface Members

    public void AddItemToQueue(
        QueItem item)
        {
            _queue.Post(item);
        }





    public async Task<QueItem> GetItemFromQueueAsync()
        {
            return await _queue.ReceiveAsync();
        }





    public bool IsQueueEmpty => _queue.Count == 0;

    #endregion

    #region Public Methods

    // Return the instance
    public static CrawlerQue Instance => _lazy.Value;

    #endregion
}