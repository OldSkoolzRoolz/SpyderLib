
using KC.Apps.SpyderLib.Logging;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;



#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
namespace KC.Apps.SpyderLib.Control;

/// <summary>
///     SpyderControl class is the main control object for the SpyderLib Library
///     The Spyder has a few options for impletentation to make a versatool tool.
///     Lib can be added to any project and initiated using the public methods in this class.
///     Spyder may also be added as a service and controlled by the built in console menu.
///     SpyderWare options can be injected using IOptions iterface during host configuration
///     or passed into the constructor. Required optoins for each mode are outlined
///     on each method.
/// </summary>
public class SpyderControl : IHostedService
{
    private readonly ICacheControl _cacheControl;
    private readonly ILogger _logger;
    private OutputControl _output = new(options: s_options);
    private KC.Apps.SpyderLib.Modules.ISpyderWeb _spyderWeb;
    private static ILoggerFactory s_factory;
    private static KC.Apps.SpyderLib.Properties.SpyderOptions s_options;





    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="factory"></param>
    public SpyderControl(IOptions<Properties.SpyderOptions> options, ILoggerFactory factory, ICacheControl cacheControl)
    {
        ArgumentNullException.ThrowIfNull(argument: options);
        ArgumentNullException.ThrowIfNull(argument: factory);


        _cacheControl = cacheControl;
        _logger = factory.CreateLogger<SpyderControl>();
        _logger.LogInformation(message: "Spyder Control and Logger Initialized");
        _spyderWeb = new KC.Apps.SpyderLib.Modules.SpyderWeb(_logger, s_options, _cacheControl);
    }





    public static KC.Apps.SpyderLib.Properties.SpyderOptions CrawlerOptions => s_options;
    public static ILoggerFactory Factory => s_factory;





    /// <summary>
    ///     Instructs Spyder to crawl each link in the input file
    /// </summary>
    /// <returns></returns>
    public async Task BeginProcessingInputFileAsync()
    {
        await _spyderWeb.ProcessInputFileAsync();

        _logger.LogInformation(message: "Tag Search Scrape operation complete");
    }





    /// <summary>
    ///     Set the depth and the starting url and crawl the web
    /// </summary>
    /// <param name="seedUrl"></param>
    public async Task BeginSpyder(string seedUrl)
    {
        var host = new Uri(uriString: seedUrl).GetLeftPart(part: UriPartial.Authority);

        try
        {
            //await web.StartSpyderAsync(startingLink: seedUrl);

            Console.WriteLine(value: "Finished crawling");
            Console.WriteLine(value: "Press Enter....");
        }
        catch (Exception e)
        {
            Console.WriteLine(value: e);
            _logger.LogError(message: "Spyder Library has an internal error crawl has been aborted");
        }
    }





    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
    }





    /// <summary>
    ///     This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The
    ///     implementation should return a task that represents
    ///     the lifetime of the long running operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">
    ///     Triggered when
    ///     <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is
    ///     called.
    /// </param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
    protected async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                PrintMenu();

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
        catch (OperationCanceledException oce)
        {
            _logger.SpyderControlException(message: "Control is shutting down gracefully");
        }
    }





    private void OnLibShutdown()
    {
        _logger.DebugTestingMessage(message: "Library shutting down");
    }





    private void PrintMenu()
    {
        string userInput;
        do
        {
            Console.Clear();
            Console.WriteLine(value: "--------------- Menu ---------------");
            Console.WriteLine(value: "1. Crawl Single Site");
            Console.WriteLine(value: "2. Process potential");
            Console.WriteLine(value: "3. Process input file");
            Console.WriteLine(value: "4. Exit");
            Console.WriteLine(value: "Enter your choice:");

            userInput = Console.ReadLine();

            switch (userInput)
            {
                case "1":
                    Console.WriteLine(value: "Starting Crawler");

                    Task.Run(function: ScrapeSingleSiteAsync);
                    Console.WriteLine(value: "Press any key to continue........");
                    Console.ReadLine();
                    break;
                case "2":
                    Console.WriteLine(value: "You've chosen Option 2");
                    Task.Run(() => _cacheControl.GetWebPageSourceAsync(address: "https://www.xvideos.com"));
                    _logger.DebugTestingMessage("### Scrape complete");
                    Console.ReadLine();
                    break;
                case "3":
                    Console.WriteLine(value: "You've chosen Option 3");
                    Task.Run(function: BeginProcessingInputFileAsync);
                    Console.ReadLine();
                    break;
                case "4":
                    Console.WriteLine(value: "Exiting the program...");
                    //_cts.Cancel();
                    break;
                default:
                    Console.WriteLine(value: "Invalid choice. Press a key to try again...");
                    Console.ReadLine();
                    break;
            }
        } while (userInput != "4");
    }





    /// Worker
    /// <summary>
    /// </summary>
    public async Task ScrapeSingleSiteAsync()
    {
        if (string.IsNullOrWhiteSpace(value: s_options.StartingUrl))
        {
            _logger.SpyderControlException(message: "Starting Url must be set");
            return;
        }

        s_options.ScrapeDepthLevel = 1;

        var newlinks = new ConcurrentScrapedUrlCollection();
        newlinks = await _spyderWeb.ScrapePageForLinksAsync(link: s_options.StartingUrl);



        var tasks = newlinks.Select(lnk => _spyderWeb.ScrapePageForHtmlTagAsync(url: lnk.Key));

        await Task.WhenAll(tasks: tasks);
    }





    /// <summary>
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        PrintMenu();
    }





    /// <summary>
    ///     Starts Crawler according to options already set during initialization
    /// </summary>
    /// <returns>Task</returns>
    public async Task StartCrawlingAsync()
    {
        await BeginSpyder(CrawlerOptions.StartingUrl);
    }





    /// <summary>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine(cancellationToken.IsCancellationRequested
                              ? "Immediate (non gracefull) exit is reqeusted"
                              : "Spyder is exiting gracefully");
        if (cancellationToken.IsCancellationRequested)
        {
            //_cts.Cancel();
        }

        return Task.CompletedTask;
    }
}