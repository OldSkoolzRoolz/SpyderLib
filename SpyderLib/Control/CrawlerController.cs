#region

using System.Collections.Concurrent;

using KC.Apps.Logging;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Models;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Logging;

#endregion

/// <summary>
///     The main interface for controlling crawler tasks
/// </summary>
public interface ICrawlerController
{
    #region Public Methods
    /// <summary>
    ///     Waits for all running crawler tasks to
    ///     finish, handles exceptions, and clears the running task list
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task CrawlAsync();





    Task StartCrawlingAsync();
    /// <summary>
    ///     Dictionary which houses all the URLs that have been scraped
    /// </summary>
    public ConcurrentDictionary<string,bool> ScrapedUrlCollection { get; }
    
    /// <summary>
    ///     Dictionary which houses all the URLs that have been newly captured
    /// </summary>
    public ConcurrentDictionary<string, bool> NewlyCapturedUrls { get; }
    

    
    /// <summary>
    ///    Initializes the crawling operation by setting up the tasks
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task SetupCrawlAsync(CancellationToken cancellationToken);

    #endregion
}




/// <summary>
///     The CrawlerController class is responsible for managing and controlling
///     crawler tasks. It provides task queueing functionality, controls
///     the amount of concurrently running tasks and provides
///     cancellation and exception management.
/// </summary>
public class CrawlerController : ICrawlerController
{
    #region Other Fields

    private readonly ICrawlerQue _crawlerQue;
    private readonly ILogger<CrawlerController> _logger;


    private readonly SpyderOptions _options;
    private readonly List<Task> _runningTasks;
    private CancellationTokenSource _crawlerCancellationTokenSource;

    // Used to prevent unnecessary scraping
    private ConcurrentDictionary<string, bool> _scrapedUrls = new ConcurrentDictionary<string, bool>();

    // Links captured during the current level of scraping
    private ConcurrentDictionary<string, bool> _newlyCapturedUrls = new ConcurrentDictionary<string, bool>();

    #endregion
    
    // Creates a dictionary for scraped urls
    public ConcurrentDictionary<string,bool> ScrapedUrlCollection => _scrapedUrls;
    public ConcurrentDictionary<string, bool> NewlyCapturedUrls => _newlyCapturedUrls;
    
    
    
    
    #region Interface Members

    /// <summary>
    ///     Control method for the running tasks. Creates a crawling task for each
    ///    concurrent crawler allowed and adds it to cref="_runningtasks" variable.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task SetupCrawlAsync(CancellationToken cancellationToken)
        {
            _crawlerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            
            int depthLevel = 1;
           

            while (depthLevel < _options.ScrapeDepthLevel)
                {
                    for (var i = 0; i < _options.ConcurrentCrawlerLimit; i++)
                        {
                            _runningTasks.Add(Task.Run(() => RunCrawlerTaskAsync(_crawlerCancellationTokenSource
                                                           .Token)));
                        }

                    //Starts all running tasks and processes any exceptions thrown
                    await CrawlAsync();
                    
                    // Processes the captured urls and prepares for next level
                   ProcessCrawledUrls();

                    depthLevel++;
                }
        }





    private void ProcessCrawledUrls()
                        {
                          

                            try
                                {
                                    if (this.NewlyCapturedUrls.IsEmpty) return;

                                    this.NewlyCapturedUrls.Keys
                                        .Where(u => !string.IsNullOrEmpty(u))
                                        .ToList() // Convert to list to allow multiple actions on IEnumerable
                                        .ForEach(u => _crawlerQue.AddItemToQueue(new QueItem(u)));


                                }
                            catch (Exception e)
                                {
                                    _logger.GeneralCrawlerError("Error cleaning up after crawler.",e);
                                }
                        
                        }





    /// <summary>
    ///     The CrawlAsync method awaits for all running crawler tasks to
    ///     finish, handles exceptions, and clears the running task list.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task CrawlAsync()
        {
            try
                {
                    await Task.WhenAll(_runningTasks.ToArray());
                }
            catch (TaskCanceledException tce)
                {
                    Log.AndContinue(tce);
                    _crawlerCancellationTokenSource.Cancel();
                }
            catch (Exception e)
                {
                    Log.AndContinue(e);
                    _crawlerCancellationTokenSource.Cancel();
                }
            finally
                {
                   
                    _runningTasks.Clear();
                }

        }

    #endregion

    #region Public Methods

    /// <summary>
    ///     The default CrawlerController constructor initializes
    ///     the logger, crawler queue, and the options for the object.
    /// </summary>
    public CrawlerController()
        {
           
            _logger = ModFactory.Instance.GetTypedLogger<CrawlerController>();
            _crawlerQue = CrawlerQue.Instance;
            //_options = ModFactory.GetSpyderOptions() ?? throw new ArgumentNullException(nameof(_options));

            _runningTasks = new List<Task>();

            // TaskCompletionSource ensures execution synchronization
            StartupComplete.TrySetResult(true);
        }





    public static TaskCompletionSource<bool> StartupComplete { get; } = new();

    #endregion




    private ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();


    public async Task StartCrawlingAsync()
        {
            var tasks = Enumerable.Range(0, _options.ConcurrentCrawlerLimit).Select(_ => Task.Run(() => WebCrawlerTaskAsync()));
            await Task.WhenAll(tasks);
        }

    private async Task WebCrawlerTaskAsync()
        {
            while (!_queue.IsEmpty)
                {
                    if (_queue.TryDequeue(out var url))
                        {
                            await ProcessUrlAsync(url);
                        }
                }
        }

    private async Task ProcessUrlAsync(string url)
        {
            // insert your web crawler logic here
            await Task.Delay(1000);   // simulating work
            Console.WriteLine($"Processed URL: {url}");
        }



    #region Private Methods

    /// <summary>
    ///     The RunCrawlerTaskAsync method represents a single crawler task running concurrently with others.
    ///     The task runs until either it's finished or cancellation is requested.
    /// </summary>
    /// <param name="token">The cancellation token used to cancel the crawler task.</param>
    /// <returns>A Task representing the ongoing crawler task.</returns>
    private async Task RunCrawlerTaskAsync(
        CancellationToken token)
        {

                  //Get crawler instance
                  PageCrawler crawler; 
            while (true)
                {
                    if (token.IsCancellationRequested || _crawlerQue.IsQueueEmpty)
                        {
                            break;
                        }


                  var nextQueItem = await _crawlerQue.GetItemFromQueueAsync();


                    try
                        {
                            //Begin crawling the Queitem set for this crawler
                          //  await crawler.BeginCrawlingSingleSiteAsync(nextQueItem);
                        }
                    catch (Exception e)
                        {
                            _logger.GeneralCrawlerError(e.Message,e);
                            
                        }
                }


        }

    #endregion
}