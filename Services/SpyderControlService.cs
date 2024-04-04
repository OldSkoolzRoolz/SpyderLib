using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

/// <summary>
///     Spyder Control Service serves as the initial loaded object and the DI object loader for SpyderLib Library
///     The Spyder has a few options for implementation to make a versatile tool.
///     This Lib is designed to be operated from a Debug app, command line or can be incorporated
///     into any AI framework. Spyder may also be added as a service and controlled by the built in Debug menu.
///     Spyder options can be injected using IOptions interface during host configuration
///     or passed into the constructor. Required options for each mode are outlined
///     on each method.
/// </summary>
[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
[SuppressMessage("ReSharper", "LocalizableElement")]
public class SpyderControlService : ServiceBase, IHostedService
{
    #region feeeldzzz

    private static string s_crawlStatus;
    private readonly IDownloadController _downControl;
    private readonly IWebCrawlerController _webCrawlerController;
    private CancellationTokenSource _cancellationTokenSource;

    #endregion






    public SpyderControlService(
        IHostApplicationLifetime lifetime,
        IWebCrawlerController webCrawlerController,
        IDownloadController downloadController) : base(lifetime)
    {
        _ = lifetime.ApplicationStarted.Register(OnStarted);

        _ = lifetime.ApplicationStopping.Register(OnStopping);
        _downControl = downloadController;
        _webCrawlerController = webCrawlerController;
    }






    #region Properteez

    public static SpyderOptions CrawlerOptions => Options;
    public static bool CrawlersActive { get; set; }



    public string LastCrawlerStatus
    {
        get => s_crawlStatus;
        set
        {
            if (value != s_crawlStatus)
            {
                _ = SetProperty(ref s_crawlStatus, value);
                RaisePropertyChanged(this.LastCrawlerStatus);
            }
        }
    }



    public static ILogger<SpyderControlService> Logger { get; set; }

    #endregion






    #region Public Methods

    public static event EventHandler LibraryHostShuttingDown;






    [MemberNotNull(nameof(_cancellationTokenSource))]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger = Factory.CreateLogger<SpyderControlService>();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Logger.SpyderInfoMessage("Spyder Control loaded");


        return Task.CompletedTask;
    }






    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            cancellationToken.IsCancellationRequested
                ? "Immediate (non gracefull) exit is reqeusted"
                : "Spyder is exiting gracefully");


        return Task.CompletedTask;
    }

    #endregion






    #region Private Methods

    private static bool CreateIfNotExists([NotNull] string path, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Directory.Exists(path))
        {
            return true;
        }

        try
        {
            var info = Directory.CreateDirectory(path);
            if (!info.Exists)
            {
                Console.WriteLine(errorMessage);
                return false;
            }
        }
        catch (UnauthorizedAccessException ua)
        {
            Logger.InternalSpyderError(ua.Message);
            return false;
        }
        catch (SpyderException)
        {
            Console.WriteLine(errorMessage);
            return false;
        }

        return true;
    }






    private void OnStarted()
    {
        if (!ValidateSpyderPathOptions(Options))
        {
            Environment.Exit(555);
        }


        Logger.SpyderInfoMessage("Waiting for all modules to load...");
        try
        {
            //Ensure all modules are loaded
            _ = Task.WhenAll(
                WebCrawlerController.StartupComplete.Task,
                DownloadController.StartupComplete.Task,
                AbstractCacheIndex.StartupComplete.Task,
                BackgroundDownloadQue.DownloadQueLoadComplete.Task,
                QueueProcessingService.QueueProcessorLoadComplete.Task).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.InternalSpyderError(
                "Error 120::A module failed to load within the timeout period. Exiting application");
            Environment.Exit(120);
        }

        Logger.SpyderInfoMessage("Dependencies loaded!");

        PrintConfig();

        _ = Task.Run(() => PrintMenu(_cancellationTokenSource!.Token));
    }






    private void OnStopping()
    {
        Console.WriteLine("output triggered");
        OutputControl.Instance.OnLibraryShutdown();
        LibraryHostShuttingDown?.Invoke(null, EventArgs.Empty);
    }






    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void PrintConfig()
    {
#pragma warning disable CA1303
        Console.WriteLine("******************************************************");
        Console.WriteLine("**             Spyder Configuration                 **");
        Console.WriteLine("**  {0,15}:   {1,-28} {2,-8}", "Output Path",
            Options.OutputFilePath, "**");
        Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Log Path", Options.LogPath,
            "**");
        Console.WriteLine(
            "**  {0,15}:   {1,-28} {2,-9}", "Cache Location", Options.CacheLocation,
            "**");

        Console.WriteLine(
            "**  {0,15}:   {1,-28} {2,-10}", "Captured Ext",
            Options.CapturedExternalLinksFilename,
            "**");

        Console.WriteLine(
            "**  {0,15}:   {1,-28} {2,-10}", "Captured Seed",
            Options.CapturedSeedUrlsFilename,
            "**");

        Console.WriteLine(
            "**  {0,15}:   {1,-28} {2,-10}", "Output FName", Options.OutputFileName,
            "**");

        Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Starting Url",
            Options.StartingUrl, "**");

        Console.WriteLine("**                                                  **");
        Console.WriteLine("******************************************************");
    }

    #endregion






    private async Task PrintMenu(
        CancellationToken cancellationToken)
    {
        string userInput;
        do
        {
            Console.WriteLine("--------------- Menu ---------------");
            Console.WriteLine("1. Start Crawler using current settings");
            Console.WriteLine("2. Search cache and download any videos");
            Console.WriteLine("3. Start crawler searching for html tag");
            Console.WriteLine("4. TBD..");
            Console.WriteLine("5. TBD..");
            Console.WriteLine("9. Exit");
            Console.WriteLine("Enter your choice:");
            userInput = Console.ReadLine();
            switch (userInput)
            {
                case "1":
                    CrawlerOptions.StartingUrl = "https://www.pornmd.com/search/a/diaper";
                    CrawlerOptions.LinkDepthLimit = 5;
                    CrawlerOptions.FollowExternalLinks = false;
                    AppContext.SetData("options", CrawlerOptions);
                    await StartCrawlingAsync(_cancellationTokenSource!.Token).ConfigureAwait(false);


                    break;

                case "2":
                    _downControl.Start();

                    break;

                case "3":
                    Console.WriteLine("Starting Search for sites with html tag");
                    CrawlerOptions.FollowExternalLinks = true;
                    CrawlerOptions.DownloadTagSource = false;
                    CrawlerOptions.EnableTagSearch = true;
                    //Save our option changes back to the appcontext for other modules.
                    AppContext.SetData("options", CrawlerOptions);

                    await StartTagSearch(_cancellationTokenSource.Token).ConfigureAwait(false);

                    break;

                case "4":
                    Debug.Write("Enter Url to download from:: ");

                    break;

                case "9":
                    // Exit scenario
                    _cancellationTokenSource.Cancel();
                    this.AppLifetime.StopApplication();

                    break;

                default:
                    Console.WriteLine("Invalid choice. Press a key to try again...");
                    _ = Console.ReadKey();

                    break;
            }
        } while (userInput != "9" && !cancellationToken.IsCancellationRequested);
    }






    private async Task StartTagSearch(CancellationToken token)
    {
        await _webCrawlerController.StartTagSearch(token).ConfigureAwait(false);
    }






    /// <summary>
    ///     Start Crawling
    /// </summary>
    public async Task StartCrawlingAsync(CancellationToken token)
    {
        await _webCrawlerController.StartCrawlingAsync(token).ConfigureAwait(false);
    }






    private static bool ValidateSpyderPathOptions(SpyderOptions options)
    {
        if (options.UseLocalCache &&
            !CreateIfNotExists(options.CacheLocation,
                "Configuration Error, cache location is not valid aborting.") || !CreateIfNotExists(options.LogPath,
                "Configuration Error, log path is not valid aborting."))
        {
            return false;
        }

        if (!CreateIfNotExists(options.OutputFilePath,
                    "Configuration Error, output path is not valid aborting."))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(options.StartingUrl))
        {
            return true;
        }

        Console.WriteLine(Resources1.ConfigError);
        return false;
    }
}