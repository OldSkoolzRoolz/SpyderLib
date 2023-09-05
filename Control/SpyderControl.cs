#region
// ReSharper disable All
using KC.Apps.Interfaces;
using KC.Apps.Models;
using KC.Apps.Modules;
using KC.Apps.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
namespace KC.Apps.Control;

/// <summary>
///     SpyderControl class is the main control object for the SpyderLib Library
///     The Spyder has a few options for impletentation to make a versatool tool.
///     Lib can be added to any project and initiated using the public methods in this class.
///     Spyder may also be added as a service and controlled by the built in console menu.
///     SpyderWare options can be injected using IOptions iterface during host configuration
///     or passed into the constructor. Required optoins for each mode are outlined
///     on each method.
/// </summary>
public class SpyderControlService : IHostedService
{
    private readonly ICacheControl _cacheControl;
    private readonly ILogger _logger;
    private OutputControl _output;
    private IBackgroundTaskQueue _taskQueue;
    private readonly ISpyderWeb _spyderWeb;





    public SpyderControlService(IOptions<SpyderOptions> options, ILoggerFactory factory, ICacheControl cacheControl,IBackgroundTaskQueue taskQueue)
    {
        ArgumentNullException.ThrowIfNull(argument: options);
        ArgumentNullException.ThrowIfNull(argument: factory);
        _taskQueue = taskQueue;
        _cacheControl = cacheControl;
        _logger = factory.CreateLogger<SpyderControlService>();
        _logger.LogInformation(message: "Spyder Control and Logger Initialized");
        _spyderWeb = new SpyderWeb(logger: _logger, options: CrawlerOptions, cacheControl: _cacheControl, _taskQueue);
    }





    public static SpyderOptions? CrawlerOptions { get; }
    public static ILoggerFactory? Factory { get; }





    public async Task BeginProcessingInputFileAsync()
    {
        await _spyderWeb.ProcessInputFileAsync();
        _logger.LogInformation(message: "Tag Search Scrape operation complete");
    }





    public async Task BeginSpyder(string seedUrl)
    {
        var host = new Uri(uriString: seedUrl).GetLeftPart(part: UriPartial.Authority);

        try
        {
            await _spyderWeb.StartSpyderAsync(startingLink: seedUrl);
            Console.WriteLine(value: "Finished crawling");
            Console.WriteLine(value: "Press Enter....");
        }
        catch (Exception)
        {
            Console.WriteLine(value: "Spyder Library has an internal error crawl has been aborted");
            _logger.LogError(message: "Spyder Library has an internal error crawl has been aborted");
        }
    }





    protected async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await PrintMenu();
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(message: "Control is shutting down gracefully");
        }
    }





    private async Task PrintMenu()
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
                    await StartCrawlingAsync();
                    break;
                case "2":
                    ProcessPotential();
                    break;
                case "3":
                    await BeginProcessingInputFileAsync();
                    break;
                case "4":
                    // Exit scenario
                    break;
                default:
                    Console.WriteLine(value: "Invalid choice. Press a key to try again...");
                    Console.ReadLine();
                    break;
            }
        } while (userInput != "4");
    }





    private void ProcessPotential()
    {
        // Implement Method
    }





    public async Task ScrapeSingleSiteAsync()
    {
        if (string.IsNullOrWhiteSpace(value: CrawlerOptions?.StartingUrl))
        {
            _logger.LogError(message: "Starting Url must be set");
            return;
        }

        var newlinks = new ConcurrentScrapedUrlCollection();
        newlinks = await _spyderWeb.ScrapePageForLinksAsync(link: CrawlerOptions.StartingUrl);
        await Task.WhenAll(newlinks.Keys.Select(url => _spyderWeb.ScrapePageForHtmlTagAsync(url: url)));
    }





    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await PrintMenu();
    }





    public async Task StartCrawlingAsync()
    {
        if (CrawlerOptions != null)
        {
            await BeginSpyder(seedUrl: CrawlerOptions.StartingUrl);
        }
    }





    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine(cancellationToken.IsCancellationRequested
                              ? "Immediate (non gracefull) exit is reqeusted"
                              : "Spyder is exiting gracefully");
        return Task.CompletedTask;
    }
}