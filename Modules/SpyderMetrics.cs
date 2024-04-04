using System.Diagnostics.Metrics;



namespace KC.Apps.SpyderLib.Modules;

public class SpyderMetrics : IDisposable
{
    #region feeeldzzz

    private readonly Meter _meter;
    private readonly Counter<int> _urlsCrawled;
    private readonly Histogram<float> _urlTimining;
    private bool _disposedValue;

    #endregion






    public SpyderMetrics(IMeterFactory meterFactory)
        {
            _meter = meterFactory.Create("Spyder.Cache");
            _urlsCrawled = _meter.CreateCounter<int>("spyder.cache.crawl-count", "{url}",
                "accumulative count of urls this session");
            _urlTimining = _meter.CreateHistogram<float>("spyder.cache.crawl-rate", "milliseconds",
                "Rate at which a single url is being handled.");
        }






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






    #region Public Methods

    public void CrawlElapsedTime(float timing)
        {
            _urlTimining.Record(timing);
        }






    public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }






    public void UrlCrawled(int qty, bool fromCache)
        {
            _urlsCrawled.Add(qty,
                fromCache
                    ? new("from.cache", null)
                    : new KeyValuePair<string, object>("from.web", null));
        }

    #endregion
}