#region

using System.Net;
using System.Security.Authentication;
using System.Text;

using KC.Apps.Logging;

using Microsoft.Extensions.Logging;

using Polly;

#endregion


namespace KC.Apps.SpyderLib.Modules;

public interface ISpyderClient
{
    #region Public Methods

    /// <summary>
    ///     Get content from the specified address with retry logic
    /// </summary>
    /// <param name="address">The web address to get the content from</param>
    /// <returns>The content obtained from the web address; an empty string if the operation fails</returns>
    Task<string> GetContentFromWebWithRetryAsync(
        string address);

    #endregion
}

/// <summary>
///     Wrapper for Http  call handling. Includes Polly retry policy with back off and jitter
/// </summary>
public class SpyderClient : ISpyderClient
{
    #region Feeelldzz

    private const int MAX_RETRY_ATTEMPTS = 3;

    private const int MAX_JITTER_DELAY_IN_MS = 1000;


    private static readonly HttpClient _httpClient = new(GetHandler())
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

    #endregion

    #region Other Fields

    private readonly ILogger _logger;

    #endregion

    #region Interface Members

    /// <summary>
    ///     Get content from the specified address with retry logic
    /// </summary>
    /// <param name="address">The web address to get the content from</param>
    /// <returns>The content obtained from the web address; an empty string if the operation fails</returns>
    public async Task<string> GetContentFromWebWithRetryAsync(
        string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                {
                    return "";
                }

            var content = string.Empty;
            try
                {
                    var response = await GetAsyncWithRetry(address).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogError($"Request to {address} failed with status code {response.StatusCode}");


                            return string.Empty;
                        }

                    content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            catch (Exception e)
                {
                    _logger.LogHttpException(e.Message, address);
                }


            return content;
        }

    #endregion

    #region Public Methods

    public SpyderClient(
        ILogger logger)
        {
            _logger = logger;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        }

    #endregion

    #region Private Methods

    private async Task<HttpResponseMessage> GetAsyncWithRetry(
        string uri)
        {
            var policy = CreateRobustPolicy(_logger);
            var result = await policy.ExecuteAsync(async () => await _httpClient.GetAsync(uri).ConfigureAwait(false))
                                     .ConfigureAwait(false);


            return result;
        }





    private static HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.All,
                    CheckCertificateRevocationList = false,

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





    private IAsyncPolicy<HttpResponseMessage> CreateRobustPolicy(
        ILogger logger)
        {
            var jitterer = new Random();

            // Retry policy
            var httpRetryPolicy = Policy<HttpResponseMessage>
                                  .Handle<HttpRequestException>()
                                  .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                                  .WaitAndRetryAsync(MAX_RETRY_ATTEMPTS,
                                                     retryAttempt =>
                                                         TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                                                         TimeSpan
                                                             .FromMilliseconds(jitterer
                                                                                   .Next(0, MAX_JITTER_DELAY_IN_MS)),
                                                     (
                                                         response,
                                                         _,
                                                         retryAttempt,
                                                         context) =>
                                                         {
                                                             logger
                                                                 .LogWarning($"Retry {retryAttempt} for Policy {context.PolicyKey} of {context.OperationKey}. due to {response.Result.StatusCode}");
                                                         });

            // Circuit breaker policy
            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                                       .Handle<HttpRequestException>()
                                       .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
                                       .CircuitBreakerAsync(
                                                            2,
                                                            TimeSpan.FromMinutes(1),
                                                            (
                                                                outcome,
                                                                breakDelay,
                                                                context) =>
                                                                {
                                                                    logger
                                                                        .LogWarning($"Circuit breaker on Policy {context.PolicyKey} of {context.OperationKey}. Break for {breakDelay.TotalMilliseconds}ms; due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                                                                },
                                                            context =>
                                                                {
                                                                    logger
                                                                        .LogInformation($"Circuit breaker on Policy {context.PolicyKey} of {context.OperationKey} Reset");
                                                                });


            // We wrap our retry policy in our CircuitBreaker policy
            return Policy.WrapAsync(circuitBreakerPolicy, httpRetryPolicy);
        }

    #endregion
}