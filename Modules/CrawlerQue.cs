using System.Threading.Tasks.Dataflow;



namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Crawler Que for QueItems using a BufferBlock
/// </summary>
internal class CrawlerQue : ICrawlerQue
{
    private readonly BufferBlock<QueItem> _queue = new();

    // Create an instance of the class with the Lazy<T> type for thread safety
    private static readonly Lazy<CrawlerQue> s_lazy = new(() => new());






    // Make the constructor private so that this class cannot be instantiated
    private CrawlerQue() { }






    #region Properteez

    // Return the instance
    public static CrawlerQue Instance => s_lazy.Value;
    public bool IsQueueEmpty => _queue.Count == 0;

    #endregion






    #region Public Methods

    public void AddItemToQueue(
        QueItem item)
        {
            _ = _queue.Post(item: item);
        }






    public Task<QueItem> GetItemFromQueueAsync()
        {
            return _queue.ReceiveAsync();
        }

    #endregion
}



internal interface ICrawlerQue
{
    #region Properteez

    bool IsQueueEmpty { get; }

    #endregion






    #region Public Methods

    void AddItemToQueue(
        QueItem item);






    Task<QueItem> GetItemFromQueueAsync();

    #endregion
}