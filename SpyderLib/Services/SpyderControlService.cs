#region

using System.Diagnostics.CodeAnalysis;

using KC.Apps.Properties;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Interfaces;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion


namespace KC.Apps.SpyderLib.Services;

/// <summary>
///     Spyder Control Service serves as the initial loaded object and the DI object loader for SpyderLib Library
///     The Spyder has a few options for implementation to make a versatile tool.
///     This Lib is designed to be operated from a console app, command line or can be incorporated
///     into any AI framework. Spyder may also be added as a service and controlled by the built in console menu.
///     Spyder options can be injected using IOptions interface during host configuration
///     or passed into the constructor. Required options for each mode are outlined
///     on each method.
/// </summary>
public class SpyderControlService : ServiceBase, IHostedService
{
    #region Other Fields

    private readonly ILogger _logger;
    private readonly ISpyderWeb _spyderWeb;
    private readonly IBackgroundDownloadQue _taskQue;
    private CancellationTokenSource _cancellationTokenSource;

 


    #endregion

    #region Interface Members

    [MemberNotNull(nameof(_cancellationTokenSource))]
    public Task StartAsync(
        CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _logger.LogInformation("Spyder Control loaded");



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

    #region Public Methods

    public SpyderControlService(
        ILoggerFactory           factory,
        IOptions<SpyderOptions>  options,
        IHostApplicationLifetime appLifetime,
        IBackgroundDownloadQue   taskQue,
        ISpyderWeb               spyderWeb) : base(factory, options.Value, appLifetime)
        {
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(factory);

            _logger = factory.CreateLogger<SpyderControlService>();
            _taskQue = taskQue;
            _spyderWeb = spyderWeb;
            // HACK Ensure property values exist
            // Must re-think DI and coupling


        }





    public static event EventHandler LibraryHostShuttingDown;



    public static bool CrawlersActive { get; set; }

    #endregion

    #region Private Methods

    /// <summary>
    /// Start Crawling
    /// </summary>
    private async Task StartCrawlingAsync(
        CancellationToken token)
        {

            if (this.Options is null || this.Options.LinkDepthLimit <= 1 || this.Options.StartingUrl is null ||
                this.Options.LogPath is null)
                {
                    _logger.LogCritical("Spyder Options Exception, Crawler aborting... Check settings and try again.");


                    return;
                }
            // Options required
            // Log Path
            // StartingUrl
            //
            //


            await _spyderWeb.StartSpyderAsync(this.Options.StartingUrl, token).ConfigureAwait(false);
        }





    private void VerifyPaths()
        {
            _logger.LogDebug("Verifying setting paths");
            try
                {
                    Directory.CreateDirectory(this.Options.CacheLocation);
                    Directory.CreateDirectory(this.Options.LogPath);
                    Directory.CreateDirectory(this.Options.OutputFilePath);
                }
            catch (Exception)
                {
                    _logger.LogCritical("Invalid file system access verifying paths. Check access to paths set in options.");
                    this.AppLifetime.StopApplication();

                }
        }





    private void OnStarted()
        {
            Console.WriteLine("Waiting for all modules to load...");
            // SpyderWeb.StartupComplete.Task.Wait();
            // CrawlerController.StartupComplete.Task.Wait();
            //            DownloadController.StartupComplete.Task.Wait();

            _logger.LogInformation("Dependencies loaded!");

            VerifyPaths();
            // RotateLogFiles();
            PrintConfig();
            Task.Run(() => PrintMenu(_cancellationTokenSource!.Token));
        }





    private void OnStopping()
        {
            Console.WriteLine("output triggered");
LibraryHostShuttingDown?.Invoke(this,EventArgs.Empty);
        }





    private void PrintConfig()
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine("**             Spyder Configuration                 **");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-8}", "Output Path", this.Options.OutputFilePath, "**");
            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Log Path", this.Options.LogPath, "**");
            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-9}", "Cache Location", this.Options.CacheLocation, "**");

            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-10}", "Captured Ext",
                              this.Options.CapturedExternalLinksFilename,
                              "**");

            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-10}", "Captured Seed", this.Options.CapturedSeedUrlsFilename,
                              "**");

            Console.WriteLine(
                              "**  {0,15}:   {1,-28} {2,-10}", "Output FName", this.Options.OutputFileName, "**");

            Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Starting Url", this.Options.StartingUrl, "**");

            Console.WriteLine("**                                                  **");
            Console.WriteLine("******************************************************");
        }





    private async Task PrintMenu(
        CancellationToken cancellationToken)
        {
            string userInput;
            do
                {
                    Console.Clear();
                    Console.WriteLine("--------------- Menu ---------------");
                    Console.WriteLine("1. Crawl Single Site");
                    Console.WriteLine("2. Process potential");
                    Console.WriteLine("3. Process input file");
                    Console.WriteLine("4. Download video tags from site..");
                    Console.WriteLine("9. Exit");
                    Console.WriteLine("Enter your choice:");
                    userInput = Console.ReadLine();
                    switch (userInput)
                        {
                            case "1":
                                await StartCrawlingAsync(_cancellationTokenSource!.Token).ConfigureAwait(false);


                                break;

                            case "2":
                                ProcessPotential();


                                break;

                            case "3":
                                Console.WriteLine("3");


                                break;

                            case "4":
                                Console.Write("Enter Url to download from:: ");
                                var url = Console.ReadLine();
                                await _spyderWeb.DownloadVideoTagsFromUrl(url).ConfigureAwait(false);


                                break;

                            case "9":
                                // Exit scenario
                                this.AppLifetime.StopApplication();


                                break;

                            default:
                                Console.WriteLine("Invalid choice. Press a key to try again...");
                                Console.ReadKey();


                                break;
                        }
                } while (userInput != "9" && !cancellationToken.IsCancellationRequested);
        }





    private void ProcessPotential()
        {
            // Implement Method
            _logger.LogInformation("potential");
        }





    /// <summary>
    ///     Method enumerates log files matching our pattern
    ///     and renames it. Also verifies the new name is not being used.
    /// </summary>
    private void RotateLogFiles()
        {
            _logger.LogTrace("Rotating Log files");
            if (!Directory.Exists(this.Options.LogPath))
                {
                    Directory.CreateDirectory(this.Options.LogPath);
                }

            var logs = Directory.GetFiles(this.Options.LogPath, "FileLogger*.log");
            foreach (var log in logs)
                {
                    var suffix = 0;
                    var newpath = log + ".old";
                    while (true)
                        {
                            if (!File.Exists(newpath))
                                {
                                    break;
                                }

                            newpath = string.Concat(newpath, suffix);
                            suffix++;
                        }

                    File.Move(log, newpath);
                }
        }

    #endregion
}