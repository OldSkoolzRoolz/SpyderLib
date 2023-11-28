using System.Diagnostics.Metrics;



namespace KC.Apps.SpyderLib.Modules;

public class SpyderMetrics : IDisposable
{
    private readonly Counter<int> _urlsCrawled;
    private readonly Histogram<float> _urlTimining;
    private bool _disposedValue;
    private Meter _meter;

    #region Interface Members

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SpyderMetrics()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }





    public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    #endregion

    #region Public Methods

    public SpyderMetrics(IMeterFactory meterFactory)
        {
            _meter = meterFactory.Create(name: "Spyder.Cache");
            _urlsCrawled = _meter.CreateCounter<int>(name: "spyder.cache.crawl-count", unit: "{url}",
                description: "accumulative count of urls this session");
            _urlTimining = _meter.CreateHistogram<float>(name: "spyder.cache.crawl-rate", unit: "milliseconds",
                description: "Rate at which a single url is being handled.");
        }





    public void CrawlElapsedTime(float timing)
        {
            _urlTimining.Record(value: timing);
        }





    public void UrlCrawled(int qty, bool fromCache)
        {
            _urlsCrawled.Add(delta: qty,
                fromCache
                    ? new(key: "from.cache", null)
                    : new KeyValuePair<string, object>(key: "from.web", null));
        }

    #endregion

    #region Private Methods

    protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
                {
                    if (disposing)
                        {
                            // TODO: dispose managed state (managed objects)
                        }

                    _meter.Dispose();
                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    _disposedValue = true;
                }
        }

    #endregion
}