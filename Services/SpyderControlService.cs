#region

using System.Diagnostics.CodeAnalysis;

using KC.Apps.Logging;
using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Modules;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#endregion




#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local

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
public class SpyderControlService : IHostedService
    {
        #region Instance variables

        private readonly IHostApplicationLifetime _appLifeTime;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private static ILoggerFactory _loggerFactory;
        private readonly ISpyderWeb _spyderWeb;

        #endregion





        public SpyderControlService(
            IOptions<SpyderOptions> options,
            IndexCacheService cache,
            IHostApplicationLifetime appLifetime)
            {
                ArgumentNullException.ThrowIfNull(options);
                CrawlerOptions = options.Value;
                _logger = LoggerFactory.CreateLogger<SpyderControlService>();
                _logger.LogInformation("Spyder Control and Logger Initialized");
                _appLifeTime = appLifetime;
                appLifetime.ApplicationStarted.Register(OnStarted);
                appLifetime.ApplicationStopping.Register(OnStopping);
                _spyderWeb = new SpyderWeb(CrawlerOptions, cache);
                Initialize();
            }





        #region Prop

        //Ensure the options are available to any class/method
        public static SpyderOptions CrawlerOptions { get; private set; }


        public static ILoggerFactory LoggerFactory
            {
                get
                {
                    if (_loggerFactory is null)
                        {
                            _loggerFactory = GetFactory(CrawlerOptions);
                        }

                    return _loggerFactory;
                }
            }

        #endregion




        #region Methods

        public async Task BeginSpyder(string seedUrl)
            {
                //var host = new Uri(uriString: seedUrl).GetLeftPart(part: UriPartial.Authority);
                try
                    {
                        await _spyderWeb.StartSpyderAsync(seedUrl).ConfigureAwait(false);
                        Console.WriteLine("Finished crawling");
                        Console.WriteLine("Press Enter....");
                    }
                catch (Exception)
                    {
                        Console.WriteLine("Spyder Library has an internal error crawl has been aborted");
                        _logger.LogError("Spyder Library has an internal error crawl has been aborted");
                    }
            }





        [MemberNotNull(nameof(_cancellationTokenSource))]
        public Task StartAsync(CancellationToken cancellationToken)
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Internal console menu launched in background thread to enable the completion of
                // the method and won't hold up any other service starting.
                /*      _ = Task.Run(
                          () =>
                              {
                                  while (!_cancellationTokenSource.IsCancellationRequested)
                                      {
      #pragma warning disable CS4014 //
                                          PrintMenu(cancellationToken);
      #pragma warning restore CS4014 //
                                      }
                              }, cancellationToken);
      */

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
                ArgumentException.ThrowIfNullOrEmpty(CrawlerOptions.StartingUrl);
                ArgumentException.ThrowIfNullOrEmpty(CrawlerOptions.LogPath);
                if (CrawlerOptions.ScrapeDepthLevel <= 0)
                    {
                        _logger.DebugTestingMessage("Scrape level must be larger than 0");
                    }

                await BeginSpyder(CrawlerOptions.StartingUrl).ConfigureAwait(false);
            }





        public Task StopAsync(CancellationToken cancellationToken)
            {
                Console.WriteLine(
                    cancellationToken.IsCancellationRequested
                        ? "Immediate (non gracefull) exit is reqeusted"
                        : "Spyder is exiting gracefully");

                return Task.CompletedTask;
            }

        #endregion




        #region Methods

        private static ILoggerFactory GetFactory(SpyderOptions options)
            {
                var ffactory = Microsoft.Extensions.Logging.LoggerFactory.Create(
                    configg =>
                        {
                            configg.ClearProviders();
                            configg.AddConsole();
                            configg.AddTextFileLogger(
                                config =>
                                    {
                                        config.EntryPrefix = "~~<";
                                        config.EntrySuffix = ">~~";
                                        config.UseUtcTime = false;
                                        config.UseSingleLogFile = true;
                                        config.TimestampFormat = "MM/dd hh:mm";
                                    });

                            configg.SetMinimumLevel(LogLevel.Debug);
                            configg.AddFilter("Microsoft", LogLevel.Information);
                        });

                return ffactory;
            }





        private void Initialize()
            {
                RotateLogFiles();
                _logger.LogTrace("Spyder Initialization Complete!");
            }





        private async void OnStarted()
            {
                PrintConfig();

                //  PrintMenu(CancellationToken.None);
                await StartCrawlingAsync().ConfigureAwait(false);
                _appLifeTime.StopApplication();
            }





        private void OnStopping()
            {
                OutputControl.OnLibraryShutdown();
                Console.WriteLine("output triggered");
            }





        private void PrintConfig()
            {
                Console.WriteLine("******************************************************");
                Console.WriteLine("**             Spyder Configuration                 **");
                Console.WriteLine("**  {0,15}:   {1,-28} {2,-8}", "Output Path", CrawlerOptions?.OutputFilePath, "**");
                Console.WriteLine("**  {0,15}:   {1,-28} {2,-10}", "Log Path", CrawlerOptions?.LogPath, "**");
                Console.WriteLine(
                    "**  {0,15}:   {1,-28} {2,-9}", "Cache Location", CrawlerOptions?.CacheLocation, "**");

                Console.WriteLine(
                    "**  {0,15}:   {1,-28} {2,-10}", "Captured Ext", CrawlerOptions?.CapturedExternalLinksFilename,
                    "**");

                Console.WriteLine(
                    "**  {0,15}:   {1,-28} {2,-10}", "Captured Seed", CrawlerOptions?.CapturedSeedUrlsFilename, "**");

                Console.WriteLine(
                    "**  {0,15}:   {1,-28} {2,-10}", "Output FName", CrawlerOptions?.OutputFileName, "**");

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
                        Console.WriteLine("--------------- Menu ---------------");
                        Console.WriteLine("1. Crawl Single Site");
                        Console.WriteLine("2. Process potential");
                        Console.WriteLine("3. Process input file");
                        Console.WriteLine("4. Exit");
                        Console.WriteLine("Enter your choice:");
                        userInput = Console.ReadLine();
                        switch (userInput)
                            {
                                case "1":
                                    await StartCrawlingAsync().ConfigureAwait(false);
                                    break;
                                case "2":
                                    ProcessPotential();
                                    break;
                                case "3":
                                    Console.WriteLine("3");
                                    break;
                                case "4":
                                    // Exit scenario
                                    _appLifeTime.StopApplication();
                                    break;
                                default:
                                    Console.WriteLine("Invalid choice. Press a key to try again...");
                                    Console.ReadLine();
                                    break;
                            }
                    } while (userInput != "4");
            }





        private void ProcessPotential()
            {
                // Implement Method
            }





        /// <summary>
        ///     Method enumerates log files matching our pattern
        ///     and renames it. Also verifies the new name is not being used.
        /// </summary>
        private void RotateLogFiles()
            {
                _logger.LogTrace("Rotating Log files");
                var logs = Directory.GetFiles(CrawlerOptions.LogPath, "FileLogger*.log");
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

                                newpath += suffix;
                                suffix++;
                            }

                        File.Move(log, newpath);
                    }
            }

        #endregion
    }