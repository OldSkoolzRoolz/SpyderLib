#region

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;

using KC.Apps.SpyderLib.Control;
using KC.Apps.SpyderLib.Logging;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

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





    Task<Stream> GetStreamAsync(Uri requestUri, CancellationToken cancellationToken);

    #endregion
}

/// <summary>
///     Wrapper for Http  call handling. Includes Polly retry policy with back off and jitter
/// </summary>
[SuppressMessage(category: "Design", checkId: "CA1031:Do not catch general exception types")]
public class SpyderClient(ILoggerFactory logger, IHttpClientFactory factory) : ISpyderClient
{
    #region Properteez

    private HttpClient Client { get; } = factory.CreateClient(name: "SpyderClient");
    private ILogger<SpyderClient> Logger { get; } = logger.CreateLogger<SpyderClient>();

    #endregion

    #region Interface Members

    /// <summary>
    ///     This is an asynchronous method that attempts to fetch content from the provided web address and handle possible
    ///     exceptions.
    /// </summary>
    /// <param name="address">
    ///     The address of the content to be accessed on the web. This must be a valid and non-empty URL
    ///     string.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The resulting value of the Task is a string which them represents:
    ///     - The content retrieved from the specified web address if the retrieval is successful.
    ///     - An empty string if the provided address is null or white space.
    ///     - A string with format "Error:{response.ReasonPhrase}", where the ReasonPhrase is the response message from a
    ///     failed HTTP request.
    /// </returns>
    /// <exception cref="SpyderCriticalException">
    ///     Thrown when an IOException occurs whilst attempting to access the content,
    ///     indicating a serious failure.
    /// </exception>
    public async Task<string> GetContentFromWebWithRetryAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(value: address))
                {
                    return string.Empty;
                }

            try
                {
                    var response = await GetAsyncWithRetry(new(uriString: address)).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                        {
                            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }

                    this.Logger.SpyderError($"Request to {address} failed with status code {response.StatusCode}");
                    _ = OutputControl.Instance.FailedCrawlerUrls.TryAdd(key: address, 1);
                    return $"Error:{response.ReasonPhrase}";
                }
            catch (HttpRequestException)
                {
                    this.Logger.SpyderWebException(message: "A critical IO collision has occurred.");

                    // Bubble up the stack to cancel overall operation
                    throw new SpyderCriticalException(
                        message: "A critical IO failure has occured during crawl. Aborting crawl operations.");
                }
            catch (OperationCanceledException oce)
                {
                    // fairly benign error we'll just log and let it be handled upstream
                    this.Logger.SpyderInfoMessage(message: oce.Message);
                }
            catch (Exception e)
                {
                    this.Logger.SpyderInfoMessage(message: e.Message);
                    throw;
                }

            return string.Empty;
        }





    public async Task<Stream> GetStreamAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return await this.Client.GetStreamAsync(requestUri: requestUri, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Performs an HTTP GET request to a specific URI with a retry policy.
    /// </summary>
    /// <param name="uri">The URI of the HTTP request.</param>
    /// <returns>
    ///     A `Task` that represents the asynchronous operation. The `Task.Result` property returns an
    ///     `HttpResponseMessage` which represents the HTTP response message including the status code and data.
    /// </returns>
    /// <example>
    ///     <code>
    /// string url = "http://example.com";
    /// var response = await GetAsyncWithRetry(url);
    /// Debug.WriteLine(response.StatusCode);
    /// </code>
    /// </example>
    /// <exception cref="HttpRequestException">Thrown when an error occurs while sending the HTTP request.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    ///     This method uses a robust retry policy which helps in handling transient faults and network issues by internally
    ///     retrying a failing request.
    ///     The retry policy is defined in the `GetRobustRetryPolicy` method.
    /// </remarks>
    private async Task<HttpResponseMessage> GetAsyncWithRetry(Uri uri)
        {
            try
                {
                    var policy = GetRobustRetryPolicy(logger: this.Logger);
                    var result = await policy.ExecuteAsync(() => this.Client.GetAsync(requestUri: uri))
                        .ConfigureAwait(false);

                    return result;
                }
            catch (Exception)
                {
                    // Polly will still throw when it has exhausted all other options.
                    //Since we have already logged any error we are going to swallow them
                }

            return default;
        }





    /// <summary>
    ///     Constructs and returns a configured instance of 'HttpClientHandler'.
    /// </summary>
    /// <remarks>
    ///     This handler is configured as follows:
    ///     - Auto redirect is enabled with a maximum of 5 automatic redirections
    ///     - All decompression methods are enabled for automatic decompression
    ///     - Certificate revocation check is disabled
    ///     - No specific network credentials are set
    ///     - Max connections per server is set to 5
    ///     - Uses the TLS 1.2 security protocol
    ///     - Cookie usage is disabled
    ///     - Does not use the default credentials
    ///     - Proxy usage is disabled
    /// </remarks>
    /// <returns>A new 'HttpClientHandler' instance with the specified settings.</returns>
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
                    UseCookies = false,
                    UseDefaultCredentials = false,
                    UseProxy = false
                };

            return handler;
        }





    private static AsyncRetryPolicy<HttpResponseMessage> GetRobustRetryPolicy(ILogger logger)
        {
            var maxRetryAttempts = 3;
            var maxJitterDelayInMs = 1000;

            // Retry policy
            var httpRetryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => msg.StatusCode >= HttpStatusCode.BadRequest)
                .WaitAndRetryAsync(retryCount: maxRetryAttempts,
                    retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, y: retryAttempt)) +
                        TimeSpan
                            .FromMilliseconds(RandomNumberGenerator
                                .GetInt32(0, toExclusive: maxJitterDelayInMs)),
                    (
                        response,
                        _,
                        retryAttempt,
                        _) =>
                        {
                            logger.SpyderWebException(
                                $"Retry {retryAttempt} for Policy. due to {response.Result.StatusCode}");
                        });




            return httpRetryPolicy;
        }

    #endregion
}