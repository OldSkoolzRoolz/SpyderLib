#region

// Resharper disable all
// File used for testing don't need code style
using Microsoft.Extensions.Logging;

using PuppeteerSharp;
using PuppeteerSharp.Mobile;

#endregion

namespace KC.Apps.Modules;

public static class TestConstants
{
    public const string ABOUT_BLANK = "about:blank";
    public const int HTTPS_PORT = PORT + 1;
    public const string HTTPS_PREFIX = "https://localhost:8082";
    public const int PORT = 8081;
    public const string SERVER_IP_URL = "http://127.0.0.1:8081";
    public const string SERVER_URL = "http://localhost:8081";
    public const string TEST_FIXTURE_COLLECTION_NAME = "PuppeteerLoaderFixture collection";
    public static readonly string CrossProcessHttpPrefix = "http://127.0.0.1:8081";
    public static readonly string EmptyPage = $"{SERVER_URL}/empty.html";
    public static readonly string CrossProcessUrl = SERVER_IP_URL;

    public static readonly string ExtensionPath =
        Path.Combine(Directory.GetCurrentDirectory(), path2: "Assets", path3: "simple-extension");

    public static readonly IEnumerable<string> NestedFramesDumpResult = new List<string>
                                                                        {
                                                                            "http://localhost:<PORT>/frames/nested-frames.html",
                                                                            "    http://localhost:<PORT>/frames/two-frames.html (2frames)",
                                                                            "        http://localhost:<PORT>/frames/frame.html (uno)",
                                                                            "        http://localhost:<PORT>/frames/frame.html (dos)",
                                                                            "    http://localhost:<PORT>/frames/frame.html (aframe)"
                                                                        };

    public static readonly DeviceDescriptor s_SroPhone = Puppeteer.Devices[key: DeviceDescriptorName.IPhone6];

    public static readonly DeviceDescriptor s_SroPhone6Landscape =
        Puppeteer.Devices[key: DeviceDescriptorName.IPhone6Landscape];

    public static string FileToUpload =>
        Path.Combine(Directory.GetCurrentDirectory(), path2: "Assets", path3: "file-to-upload.txt");

    public static ILoggerFactory? LoggerFactory { get; }





    public static LaunchOptions BrowserWithExtensionOptions()
    {
        return new()
               {
                   Headless = false,
                   Args = new[]
                          {
                              $"--disable-extensions-except={ExtensionPath}",
                              $"--load-extension={ExtensionPath}"
                          }
               };
    }





    public static LaunchOptions DefaultBrowserOptions() => new()
                                                           {
                                                               SlowMo =
                                                                   Convert.ToInt32(Environment
                                                                       .GetEnvironmentVariable(variable:
                                                                           "SLOW_MO")),
                                                               Headless =
                                                                   Convert.ToBoolean(Environment
                                                                       .GetEnvironmentVariable(variable:
                                                                           "HEADLESS") ?? "true"),
                                                               Timeout = 0,
                                                               LogProcess = true,
                                                               #if NETCOREAPP
                                                               EnqueueTransportMessages = false
                                                               #else
                                                               EnqueueTransportMessages = true
                                                               #endif
                                                           };
}