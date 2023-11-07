namespace KC.Apps.SpyderLib.Services;

public sealed class CacheSignal
{
    #region Other Fields

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    #endregion

    #region Public Methods

    /// <summary>
    ///     Exposes the ability to signal the release of the <see cref="WaitAsync" />'s operation.
    ///     Callers who were waiting, will be able to continue.
    /// </summary>
    public void Release()
        {
            _semaphore.Release();
        }





    /// <summary>
    ///     Exposes a <see cref="Task" /> that represents the asynchronous wait operation.
    ///     When signaled (consumer calls <see cref="Release" />), the
    ///     <see cref="Task.Status" /> is set as <see cref="TaskStatus.RanToCompletion" />.
    /// </summary>
    public Task WaitAsync()
        {
            return _semaphore.WaitAsync();
        }

    #endregion
}