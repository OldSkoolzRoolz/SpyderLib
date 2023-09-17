#region

// ReSharper disable All
using System.Diagnostics.CodeAnalysis;

using KC.Apps.Interfaces;
using KC.Apps.Logging;
using KC.Apps.Models;
using KC.Apps.Modules;
using KC.Apps.Properties;
using KC.Apps.SpyderLib.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
namespace KC.Apps.Control;



public interface ISpyderControlService
{
    Task BeginSpyder(string seedUrl);
    Task StartAsync(CancellationToken cancellationToken);
    Task StartCrawlingAsync();
    Task StopAsync(CancellationToken cancellationToken);
}




/// <summary>
///     SpyderControl class is the main control object for the SpyderLib Library
///     The Spyder has a few options for impletentation to make a versatool tool.
///     Lib can be added to any project and initiated using the public methods in this class.
///     Spyder may also be added as a service and controlled by the built in console menu.
///     SpyderWare options can be injected using IOptions iterface during host configuration
///     or passed into the constructor. Required optoins for each mode are outlined
///     on each method.
/// </summary>
public class SpyderControlService : ISpyderControlService, IHostedService
{
    private readonly IndexCacheService _cacheControl;

    private readonly ILogger _logger;
    private readonly ISpyderWeb _spyderWeb;
    private readonly IBackgroundTaskQueue _taskQueue;


    private CancellationTokenSource _cancellationTokenSource;

    private OutputControl _output = null;





    public SpyderControlService(
        IOptions<SpyderOptions> options,
        ILoggerFactory factory,
        IndexCacheService cacheControl,
        IHostApplicationLifetime appLifetime)
        {
            ArgumentNullException.ThrowIfNull(argument: options);
            ArgumentNullException.ThrowIfNull(argument: factory);
            CrawlerOptions = options.Value;
            _output = new(CrawlerOptions ?? throw new InvalidOperationException("CrawlerOptions is null"));
            _cacheControl = cacheControl;
            _logger = factory.CreateLogger<SpyderControlService>();
            _logger.LogInformation(message: "Spyder Control and Logger Initialized");
            _spyderWeb = new SpyderWeb(logger: _logger, options, _cacheControl);
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
        }





    public static SpyderOptions? CrawlerOptions { get; private set; }
    public static ILoggerFactory? Factory { get; }





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





    [MemberNotNull(nameof(_cancellationTokenSource))]
    public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(
                () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        PrintMenu(cancellationToken);
                    }
                });

            return Task.CompletedTask;
        }





    /// <summary>
    /// </summary>
    public async Task StartCrawlingAsync()
        {
            // Options required
            // Log Path
            // StartingUrl
            //
            //
            ArgumentNullException.ThrowIfNull(CrawlerOptions);
            ArgumentNullException.ThrowIfNullOrEmpty(CrawlerOptions.StartingUrl);
            ArgumentNullException.ThrowIfNullOrEmpty(CrawlerOptions.LogPath);
            if (CrawlerOptions.ScrapeDepthLevel <= 0)
            {
                _logger.DebugTestingMessage("Scrape level must be larger than 0");
            }

            await BeginSpyder(seedUrl: CrawlerOptions.StartingUrl);
        }





    public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(
                cancellationToken.IsCancellationRequested
                    ? "Immediate (non gracefull) exit is reqeusted"
                    : "Spyder is exiting gracefully");

            return Task.CompletedTask;
        }





    private void OnStopping()
        {
            _output.OnLibraryShutdown();
        }





    private void OnStarted()
        {
            PrintConfig();
        }





    private void PrintConfig()
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine("**             Spyder Configuration                 **");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-8}", "Output Path", CrawlerOptions?.OutputFilePath, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Log Path", CrawlerOptions?.LogPath, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-9}", "Cache Location", CrawlerOptions?.CacheLocation, "**");
            Console.WriteLine(
                "**  {0,15}:   {1,-28} {2,-10}", "Captured Ext", CrawlerOptions?.CapturedExternalLinksFilename, "**");

            Console.WriteLine(
                "**  {0,15}:   {1,-28} {2,-10}", "Captured Seed", CrawlerOptions?.CapturedSeedUrlsFilename, "**");

            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Output FName", CrawlerOptions?.OutputFileName, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Starting Url", CrawlerOptions?.StartingUrl, "**");
            Console.WriteLine("**                                                  **");
            Console.WriteLine("******************************************************");
        }





    private async Task PrintMenu(CancellationToken cancellationToken)
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
                        Console.WriteLine("3");
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
}