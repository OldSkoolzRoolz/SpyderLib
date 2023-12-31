using System.Diagnostics.Metrics;



namespace KC.Apps.SpyderLib.Modules;

public class SpyderMetrics : IDisposable
{
    private bool _disposedValue;
    private readonly Meter _meter;
    private readonly Counter<int> _urlsCrawled;
    private readonly Histogram<float> _urlTimining;






    public SpyderMetrics(IMeterFactory meterFactory)
        {
            _meter = meterFactory.Create(name: "Spyder.Cache");
            _urlsCrawled = _meter.CreateCounter<int>(name: "spyder.cache.crawl-count", unit: "{url}",
                description: "accumulative count of urls this session");
            _urlTimining = _meter.CreateHistogram<float>(name: "spyder.cache.crawl-rate", unit: "milliseconds",
                description: "Rate at which a single url is being handled.");
        }






    #region Public Methods

    public void CrawlElapsedTime(float timing)
        {
            _urlTimining.Record(value: timing);
        }






    public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
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
                            // toODO: dispose managed state (managed objects)
                        }

                    _meter.Dispose();
                    // toODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // toODO: set large fields to null
                    _disposedValue = true;
                }
        }

    #endregion
}