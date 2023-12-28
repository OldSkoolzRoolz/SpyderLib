using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
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
public class SpyderControlService : ServiceBase, IHostedService
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly IDownloadController _downControl;
    private readonly IWebCrawlerController _webCrawlerController;
    private static string s_crawlStatus;






    public SpyderControlService(
        IHostApplicationLifetime lifetime,
        IWebCrawlerController webCrawlerController,
        IDownloadController downloadController) : base(lifetime: lifetime)
        {
            //TODO Replace host lifetime with internal lifetime control
            lifetime.ApplicationStarted.Register(callback: OnStarted);
            lifetime.ApplicationStopping.Register(callback: OnStopping);
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
                            SetProperty(field: ref s_crawlStatus, value: value);
                            RaisePropertyChanged(propertyName: this.LastCrawlerStatus);
                        }
                }
        }



    public static ILogger<SpyderControlService> Logger { get; set; }

    #endregion






    #region Public Methods

    public static event EventHandler LibraryHostShuttingDown;






    [MemberNotNull(nameof(_cancellationTokenSource))]
    public Task StartAsync(
        CancellationToken cancellationToken)
        {
            Logger = Factory.CreateLogger<SpyderControlService>();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token: cancellationToken);
            Logger.SpyderInfoMessage(message: "Spyder Control loaded");


            return Task.CompletedTask;
        }






    public Task StopAsync(
        CancellationToken cancellationToken)
        {
            Debug.WriteLine(
                cancellationToken.IsCancellationRequested
                    ? "Immediate (non gracefull) exit is reqeusted"
                    : "Spyder is exiting gracefully");


            return Task.CompletedTask;
        }

    #endregion






    #region Private Methods

    private static bool CreateIfNotExists([NotNull] string path, string errorMessage)
        {
            ArgumentNullException.ThrowIfNull(argument: path);

            if (Directory.Exists(path: path))
                {
                    return true;
                }

            try
                {
                    var info = Directory.CreateDirectory(path: path);
                    if (!info.Exists)
                        {
                            Debug.WriteLine(message: errorMessage);
                            return false;
                        }
                }
            catch (UnauthorizedAccessException ua)
                {
                    Logger.InternalSpyderError(message: ua.Message);
                    return false;
                }
            catch (SpyderException)
                {
                    Debug.WriteLine(message: errorMessage);
                    return false;
                }

            return true;
        }






    private void OnStarted()
        {
            if (!ValidateSpyderPathOptions(options: Options))
                {
                    Environment.Exit(555);
                }


            Logger.SpyderInfoMessage(message: "Waiting for all modules to load...");
            try
                {
                    //Ensure all modules are loaded
                    Task.WhenAll(
                        WebCrawlerController.StartupComplete.Task,
                        DownloadController.StartupComplete.Task,
                        AbstractCacheIndex.StartupComplete.Task,
                        BackgroundDownloadQue.DownloadQueLoadComplete.Task,
                        QueueProcessingService.QueueProcessorLoadComplete.Task).ConfigureAwait(false);
                }
            catch (TimeoutException)
                {
                    Logger.InternalSpyderError(
                        message: "Error 120::A module failed to load within the timeout period. Exiting application");
                    Environment.Exit(120);
                }

            Logger.SpyderInfoMessage(message: "Dependencies loaded!");

            PrintConfig();

            _ = Task.Run(() => PrintMenu(cancellationToken: _cancellationTokenSource!.Token));

            // Task.Run(() => Task.FromResult(StartCrawlingAsync(_cancellationTokenSource.Token))).ConfigureAwait(false);
        }






    private void OnStopping()
        {
            Debug.WriteLine(message: "output triggered");
            LibraryHostShuttingDown?.Invoke(this, e: EventArgs.Empty);
        }






    private void PrintConfig()
        {
#pragma warning disable CA1303
            Console.WriteLine(value: "******************************************************");
            Console.WriteLine(value: "**             Spyder Configuration                 **");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-8}", arg0: "Output Path",
                arg1: Options.OutputFilePath, arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Log Path", arg1: Options.LogPath,
                arg2: "**");
            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-9}", arg0: "Cache Location", arg1: Options.CacheLocation,
                arg2: "**");

            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Captured Ext",
                arg1: Options.CapturedExternalLinksFilename,
                arg2: "**");

            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Captured Seed",
                arg1: Options.CapturedSeedUrlsFilename,
                arg2: "**");

            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Output FName", arg1: Options.OutputFileName,
                arg2: "**");

            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Starting Url",
                arg1: Options.StartingUrl, arg2: "**");

            Console.WriteLine(value: "**                                                  **");
            Console.WriteLine(value: "******************************************************");
        }

    #endregion






    private async Task PrintMenu(
        CancellationToken cancellationToken)
        {
            string userInput;
            do
                {
                    Console.WriteLine(value: "--------------- Menu ---------------");
                    Console.WriteLine(value: "1. Start Crawler");
                    Console.WriteLine(value: "2. Scan cache for html tags");
                    Console.WriteLine(value: "3. Process input file");
                    Console.WriteLine(value: "4. Download video tags from site..");
                    Console.WriteLine(value: "9. Exit");
                    Console.WriteLine(value: "Enter your choice:");
                    userInput = Console.ReadLine();
                    switch (userInput)
                        {
                            case "1":
                                await StartCrawlingAsync(token: _cancellationTokenSource!.Token).ConfigureAwait(false);


                                break;

                            case "2":
                                _downControl.Start();

                                break;

                            case "3":
                                Debug.WriteLine(message: "3");


                                break;

                            case "4":
                                Debug.Write(message: "Enter Url to download from:: ");

                                break;

                            case "9":
                                // Exit scenario
                                Environment.Exit(0);


                                break;

                            default:
                                Debug.WriteLine(message: "Invalid choice. Press a key to try again...");
                                _ = Console.ReadKey();


                                break;
                        }
                } while (userInput != "9" && !cancellationToken.IsCancellationRequested);
        }






    /// <summary>
    ///     Start Crawling
    /// </summary>
    public async Task StartCrawlingAsync(CancellationToken token)
        {
            await _webCrawlerController.StartCrawlingAsync(token: token).ConfigureAwait(false);
        }






    private static bool ValidateSpyderPathOptions(SpyderOptions options)
        {
            if (options.UseLocalCache &&
                !CreateIfNotExists(path: options.CacheLocation,
                    errorMessage: "Configuration Error, cache location is not valid aborting."))
                {
                    return false;
                }

            if (!CreateIfNotExists(path: options.LogPath,
                    errorMessage: "Configuration Error, log path is not valid aborting."))
                {
                    return false;
                }

            if (!CreateIfNotExists(path: options.OutputFilePath,
                    errorMessage: "Configuration Error, output path is not valid aborting."))
                {
                    return false;
                }

            if (!string.IsNullOrWhiteSpace(value: options.StartingUrl))
                {
                    return true;
                }

            Debug.WriteLine(message: Resources1.ConfigError);
            return false;
        }
}