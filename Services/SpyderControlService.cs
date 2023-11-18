#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Extensions;
using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

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
    private static ILogger s_logger;
    private readonly ISpyderWeb _spyderWeb;
    private CancellationTokenSource _cancellationTokenSource;
    private static SpyderOptions s_options;
    private readonly TextFileLoggerConfiguration _loggerConfig;

    #region Interface Members

    [MemberNotNull(nameof(_cancellationTokenSource))]
    public Task StartAsync(
        CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            s_logger.LogInformation("Spyder Control loaded");


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
        ILoggerFactory factory,
        IOptions<SpyderOptions> options,
        IOptions<TextFileLoggerConfiguration> logconfig,
        IHostApplicationLifetime appLifetime,
        ISpyderWeb spyderWeb) : base(factory, options.Value, appLifetime)
        {
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(factory);
            s_options = options.Value;
            s_logger = factory.CreateLogger<SpyderControlService>();
            _spyderWeb = spyderWeb;
            _loggerConfig = logconfig.Value;
        }





    public static SpyderOptions CrawlerOptions { get; set; }
    public static bool CrawlersActive { get; set; }
    public static event EventHandler LibraryHostShuttingDown;

    #endregion

    #region Private Methods

    /// <summary>
    ///     Start Crawling
    /// </summary>
    private Task StartCrawlingAsync(
        CancellationToken token)
        {
            if (this.Options is not null && this.Options.LinkDepthLimit > 1 && this.Options.StartingUrl is not null &&
                this.Options.LogPath is not null) return _spyderWeb.StartSpyderAsync(this.Options.StartingUrl, token);
            s_logger.LogCritical("Spyder Options Exception, Crawler aborting... Check settings and try again.");


            return Task.CompletedTask;
            // Options required
            // Log Path
            // StartingUrl
            //
            //
        }





    private static bool CreateIfNotExists([NotNull] string path, string errorMessage)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (Directory.Exists(path))
                {
                    return true;
                }

            DirectoryInfo info = null;
            try
                {
                    info = Directory.CreateDirectory(path);
                    if (!info.Exists)
                        {
                            Console.WriteLine(errorMessage);
                            return false;
                        }
                }
            catch (UnauthorizedAccessException ua)
                {
                   s_logger.LogError(ua.Message);
                    return false;
                }
            catch (Exception)
                {
                    Console.WriteLine(errorMessage);
                    return false;
                }

            return true;
        }





    private void OnStarted()
        {
            if (!ValidateSpyderPathOptions(s_options, _loggerConfig))
                {
                    Environment.Exit(555);
                }

            CrawlerOptions = s_options;
            s_logger.LogInformation("Waiting for all modules to load...");
            //Ensure all modules are loaded
            Task.WhenAll(SpyderWeb.StartupComplete.Task,
                //DownloadController.StartupComplete.Task,
                CacheIndexService.StartupComplete.Task).WithTimeout(TimeSpan.FromMinutes(2)).ConfigureAwait(false);

            s_logger.LogInformation("Dependencies loaded!");

            // RotateLogFiles();
            PrintConfig();

            Task.Run(() => Task.FromResult(PrintMenu(_cancellationTokenSource!.Token)));
           // Task.Run(() => Task.FromResult(StartCrawlingAsync(_cancellationTokenSource.Token))).ConfigureAwait(false);
        }





    private static bool ValidateSpyderPathOptions(SpyderOptions options, TextFileLoggerConfiguration config)
        {
            if (options.UseLocalCache && !CreateIfNotExists(options.CacheLocation,
                    "Configuration Error, cache location is not valid aborting."))
                {
                    return false;
                }

            if (!CreateIfNotExists(config.LogLocation, "Configuration Error, log path is not valid aborting."))
                {
                    return false;
                }

            if (!CreateIfNotExists(options.OutputFilePath, "Configuration Error, output path is not valid aborting."))
                {
                    return false;
                }

            if (!string.IsNullOrWhiteSpace(options.StartingUrl)) return true;
            Console.WriteLine("Configuration Error,Starting url is invalid. aborting.");
            return false;
        }





    private void OnStopping()
        {
            Console.WriteLine("output triggered");
            LibraryHostShuttingDown?.Invoke(this, EventArgs.Empty);
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
                "**  {0,15}:   {1,-28} {2,-10}", "Captured Ext", this.Options.CapturedExternalLinksFilename,
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
            s_logger.LogInformation("potential");
        }





    /// <summary>
    ///     Method enumerates log files matching our pattern
    ///     and renames it. Also verifies the new name is not being used.
    /// </summary>
    private void RotateLogFiles()
        {
            s_logger.LogTrace("Rotating Log files");
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