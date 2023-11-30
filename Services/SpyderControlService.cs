#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Interfaces;
using KC.Apps.SpyderLib.Logging;
using KC.Apps.SpyderLib.Modules;
using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TaskExtensions = KC.Apps.SpyderLib.Extensions.TaskExtensions;



// ReSharper disable LocalizableElement

#endregion

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
    private static ILogger s_logger;
    private static SpyderOptions s_options;
    private readonly TextFileLoggerConfiguration _loggerConfig;
    private readonly ISpyderWeb _spyderWeb;
    private CancellationTokenSource _cancellationTokenSource;

    #region Interface Members

    [MemberNotNull(nameof(_cancellationTokenSource))]
    public Task StartAsync(
        CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token: cancellationToken);
            s_logger.SpyderInfoMessage(message: "Spyder Control loaded");


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

    #region Public Methods

    public SpyderControlService(
        ILoggerFactory factory,
        IOptions<SpyderOptions> options,
        IOptions<TextFileLoggerConfiguration> logconfig,
        IHostApplicationLifetime appLifetime,
        ISpyderWeb spyderWeb) : base(factory: factory, options: options.Value, lifetime: appLifetime)
        {
            Guard.IsNotNull(value: options);
            ArgumentNullException.ThrowIfNull(argument: factory);

            ArgumentNullException.ThrowIfNull(argument: options);

            ArgumentNullException.ThrowIfNull(argument: logconfig);


            if (appLifetime != null)
                {
                    _ = appLifetime.ApplicationStarted.Register(callback: OnStarted);
                    _ = appLifetime.ApplicationStopping.Register(callback: OnStopping);
                }


            s_options = options.Value;
            s_logger = factory.CreateLogger<SpyderControlService>();
            _spyderWeb = spyderWeb ?? throw new ArgumentNullException(nameof(spyderWeb));
            _loggerConfig = logconfig.Value;
        }





    public static SpyderOptions CrawlerOptions { get; set; }
    public static bool CrawlersActive { get; set; }
    public static event EventHandler LibraryHostShuttingDown;
    public static ILogger Logger { get; } = s_logger;

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
                            Debug.WriteLine(value: errorMessage);
                            return false;
                        }
                }
            catch (UnauthorizedAccessException ua)
                {
                    s_logger.InternalSpyderError(message: ua.Message);
                    return false;
                }
            catch (SpyderException)
                {
                    Debug.WriteLine(value: errorMessage);
                    return false;
                }

            return true;
        }





    private void OnStarted()
        {
            if (!ValidateSpyderPathOptions(options: s_options, config: _loggerConfig))
                {
                    Environment.Exit(555);
                }

            CrawlerOptions = s_options;
            s_logger.SpyderInfoMessage(message: "Waiting for all modules to load...");
            //Ensure all modules are loaded
            _ = TaskExtensions.WithTimeout(Task.WhenAll(SpyderWeb.StartupComplete.Task,
                //DownloadController.StartupComplete.Task,
                CacheIndexService.StartupComplete.Task), TimeSpan.FromMinutes(2)).ConfigureAwait(false);

            s_logger.SpyderInfoMessage(message: "Dependencies loaded!");

            // RotateLogFiles();
            PrintConfig();

            _ = Task.Run(() => PrintMenu(cancellationToken: _cancellationTokenSource!.Token));
            // Task.Run(() => Task.FromResult(StartCrawlingAsync(_cancellationTokenSource.Token))).ConfigureAwait(false);
        }





    private void OnStopping()
        {
            Debug.WriteLine(value: "output triggered");
            LibraryHostShuttingDown?.Invoke(this, e: EventArgs.Empty);
        }





    private void PrintConfig()
        {
#pragma warning disable CA1303
            Console.WriteLine(value: "******************************************************");
            Console.WriteLine(value: "**             Spyder Configuration                 **");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-8}", arg0: "Output Path",
                arg1: this.Options.OutputFilePath, arg2: "**");
            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Log Path", arg1: this.Options.LogPath,
                arg2: "**");
            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-9}", arg0: "Cache Location", arg1: this.Options.CacheLocation,
                arg2: "**");

            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Captured Ext",
                arg1: this.Options.CapturedExternalLinksFilename,
                arg2: "**");

            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Captured Seed",
                arg1: this.Options.CapturedSeedUrlsFilename,
                arg2: "**");

            Console.WriteLine(
                format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Output FName", arg1: this.Options.OutputFileName,
                arg2: "**");

            Console.WriteLine(format: "**  {0,15}:   {1,-28} {2,-10}", arg0: "Starting Url",
                arg1: this.Options.StartingUrl, arg2: "**");

            Console.WriteLine(value: "**                                                  **");
            Console.WriteLine(value: "******************************************************");
        }

    #endregion

    #endregion





    private async Task PrintMenu(
        CancellationToken cancellationToken)
        {
            string userInput;
            do
                {
                    Debug.WriteLine(value: "--------------- Menu ---------------");
                    Debug.WriteLine(value: "1. Crawl Single Site");
                    Debug.WriteLine(value: "2. Scan cache for html tags");
                    Debug.WriteLine(value: "3. Process input file");
                    Debug.WriteLine(value: "4. Download video tags from site..");
                    Debug.WriteLine(value: "9. Exit");
                    Debug.WriteLine(value: "Enter your choice:");
                    userInput = Console.ReadLine();
                    switch (userInput)
                        {
                            case "1":
                                await StartCrawlingAsync(token: _cancellationTokenSource!.Token).ConfigureAwait(false);


                                break;

                            case "2":
                                _spyderWeb.SearchLocalCacheForTags();


                                break;

                            case "3":
                                Debug.WriteLine(value: "3");


                                break;

                            case "4":
                                Debug.Write(value: "Enter Url to download from:: ");

                                break;

                            case "9":
                                // Exit scenario
                                this.AppLifetime.StopApplication();


                                break;

                            default:
                                Debug.WriteLine(value: "Invalid choice. Press a key to try again...");
                                _ = Console.ReadKey();


                                break;
                        }
                } while (userInput != "9" && !cancellationToken.IsCancellationRequested);
        }





    /// <summary>
    ///     Start Crawling
    /// </summary>
    private Task StartCrawlingAsync(
        CancellationToken token)
        {
            if (this.Options is not null &&
                this.Options.LinkDepthLimit > 1 &&
                this.Options.StartingUrl is not null &&
                this.Options.LogPath is not null)
                {
                    return _spyderWeb.StartSpyderAsync(startingLink: this.Options.StartingUrl, token: token);
                }

            s_logger.CriticalOptions(
                message: "Spyder Options Exception, Crawler aborting... Check settings and try again.");


            return Task.CompletedTask;
            // Options required
            // Log Path
            // StartingUrl
            //
            //
        }





    private static bool ValidateSpyderPathOptions(SpyderOptions options, TextFileLoggerConfiguration config)
        {
            if (options.UseLocalCache &&
                !CreateIfNotExists(path: options.CacheLocation,
                    errorMessage: "Configuration Error, cache location is not valid aborting."))
                {
                    return false;
                }

            if (!CreateIfNotExists(path: config.LogLocation,
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

            Debug.WriteLine(value: Resources1.ConfigError);
            return false;
        }
}