#region

using System.Net;
using System.Security.Authentication;
using KC.Apps.Logging;
using KC.Apps.SpyderLib.Control;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

#endregion


namespace KC.Apps.SpyderLib.Modules;

/// <summary>
///     Wrapper for Http  call handling. Includes Pollu retry policy with back off and jitter
/// </summary>
public class MyHttpClient
{
    private readonly ILogger _logger;





    public MyHttpClient(ILogger logger)
        {
            _logger = logger;



        }





    private static readonly HttpClient _httpClient = new(GetHandler());





    public async Task<string> GetContentFromWebWithRetryAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                {
                    return "";
                }

            HttpResponseMessage response = null;
            var content = string.Empty;
            try
                {
                    response = await GetAsyncWithRetry(address);

                    if (!response.IsSuccessStatusCode)
                        {
                            OutputControl.FailedCrawlerUrls.TryAdd(address, 1);
                            _logger.LogError($"Request to {address} failed with status code {response.StatusCode}");
                            return string.Empty;
                        }

                    content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            catch (Exception e)
                {
                    _logger.LogHttpException(e.Message);
                }

            return content;
        }





    public async Task<HttpResponseMessage> GetAsyncWithRetry(string uri)
        {
            var policy = CreateRobustRetryPolicy(_logger);
            var result = await policy.ExecuteAsync(async () => await _httpClient.GetAsync(uri));
            return result;
        }





    private static HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.All,
                    CheckCertificateRevocationList = false,
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    Credentials = null,
                    DefaultProxyCredentials = null,
                    MaxAutomaticRedirections = 5,
                    MaxConnectionsPerServer = 5,
                    SslProtocols = SslProtocols.Tls12,
                    UseCookies = false,
                    UseDefaultCredentials = false,
                    UseProxy = false
                };

            return handler;
        }





    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                   .HandleTransientHttpError()
                   .Or<TimeoutRejectedException>() // thrown by Polly's TimeoutPolicy if the call timed out
                   .WaitAndRetryAsync(
                                      // number of retries
                                      3,
                                      // exponential backoff
                                      retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                      // on retry
                                      (exception, timeSpan, retryCount, context) =>
                                          {
                                              logger
                                                  .LogDebug($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
                                          });
        }





    public IAsyncPolicy<HttpResponseMessage> CreateRobustRetryPolicy(ILogger logger)
        {
            var jitterer = new Random();

            var policy = HttpPolicyExtensions
                         .HandleTransientHttpError() // Handles HttpRequestExceptions, and 500 series status codes
                         .OrResult(response =>
                                       (int)response.StatusCode == 429) // Handle 429 / Rate Limit Exceeded status code
                         .WaitAndRetryAsync(
                                            3, // Retry 5 times
                                            retryAttempt =>
                                                TimeSpan
                                                    .FromSeconds(Math
                                                                     .Pow(2,
                                                                          retryAttempt)) // Exponential back-off (2, 4, 8, 16 etc)
                                                + TimeSpan
                                                    .FromMilliseconds(jitterer
                                                                          .Next(0,
                                                                                1000)), // Plus jitter (random delay to avoid thundering herd problem)
                                            (response, delay, retryCount, context) =>
                                                {
                                                    if (response.Exception != null)
                                                        {
                                                            // Log all exceptions between retries
                                                            logger
                                                                .LogError($"Retry {retryCount} Delay {delay} Exception {response.Exception.Message}",
                                                                          response.Exception);
                                                        }
                                                    else if (response.Result != null)
                                                        {
                                                            // Log all faulted results
                                                        }
                                                });

            return policy;
        }
}