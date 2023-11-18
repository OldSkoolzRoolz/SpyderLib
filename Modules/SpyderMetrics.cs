using System.Diagnostics.Metrics;



namespace KC.Apps.SpyderLib.Modules;

public class SpyderMetrics
{
    private readonly Counter<int> _urlsCrawled;
    private readonly Histogram<float> _urlTimining;


    public SpyderMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("Spyder.Cache");
            _urlsCrawled = meter.CreateCounter<int>("spyder.cache.crawlcount","{url}","accumulative count of urls this session");
            _urlTimining = meter.CreateHistogram<float>("spyder.cache.crawlrate","milliseconds","Rate at which a single url is being handled.");
        }

    public void UrlCrawled(int qty, bool fromCache)
        {
            if(fromCache)
                {
                    _urlsCrawled.Add(qty,new KeyValuePair<string, object>("from.cache",null));
                }
            else
                {
                    _urlsCrawled.Add(qty,new KeyValuePair<string, object>("from.web",null));
                }

        }

    public void CrawlElapsedTime(float timing)
        {
            _urlTimining.Record(timing);
        }


}