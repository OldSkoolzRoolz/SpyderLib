#region

using System.Net;

using KC.Apps.SpyderLib.Control;

using Microsoft.Extensions.Logging;

#endregion




namespace KC.Apps.Modules;




public interface IMyHttpClient
    {
        #region Methods

        Task<string> GetHttpContentFromWebAsync(string address);

        #endregion
    }




public class MyHttpClient : IMyHttpClient
    {
        #region Instance variables

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _namedClient;

        #endregion





        public MyHttpClient(
            HttpClient httpClient,
            ILogger<MyHttpClient> logger)
            {
                _httpClient = httpClient;
                _logger = logger;
            }





        #region Methods

        public async Task<string> GetHttpContentFromWebAsync(string address)
            {
                if (string.IsNullOrWhiteSpace(address))
                    {
                        throw new ArgumentNullException(nameof(address), "Address cannot be null or empty.");
                    }


                try
                    {
                        var response = await _httpClient.GetAsync(address, HttpCompletionOption.ResponseContentRead)
                            .ConfigureAwait(false);

                        switch (response.StatusCode)
                            {
                                case HttpStatusCode.Continue:
                                    break;

                                case HttpStatusCode.OK:
                                    break;
                                case HttpStatusCode.Created:
                                    break;
                                case HttpStatusCode.Accepted:
                                    break;
                                case HttpStatusCode.NonAuthoritativeInformation:
                                    break;
                                case HttpStatusCode.NoContent:
                                    break;
                                case HttpStatusCode.ResetContent:
                                    break;
                                case HttpStatusCode.PartialContent:
                                    break;
                                case HttpStatusCode.MultiStatus:
                                    break;
                                case HttpStatusCode.AlreadyReported:
                                    break;
                                case HttpStatusCode.IMUsed:
                                    break;
                                case HttpStatusCode.Ambiguous:
                                    break;
                                case HttpStatusCode.Moved:
                                    break;
                                case HttpStatusCode.Found:
                                    break;
                                case HttpStatusCode.RedirectMethod:
                                    break;
                                case HttpStatusCode.NotModified:
                                    break;
                                case HttpStatusCode.UseProxy:
                                    break;
                                case HttpStatusCode.Unused:
                                    break;
                                case HttpStatusCode.RedirectKeepVerb:
                                    break;
                                case HttpStatusCode.PermanentRedirect:
                                    break;
                                case HttpStatusCode.BadRequest:
                                    break;
                                case HttpStatusCode.Unauthorized:
                                    break;
                                case HttpStatusCode.PaymentRequired:
                                    break;
                                case HttpStatusCode.Forbidden:
                                    break;
                                case HttpStatusCode.NotFound:
                                    break;
                                case HttpStatusCode.MethodNotAllowed:
                                    break;
                                case HttpStatusCode.NotAcceptable:
                                    break;
                                case HttpStatusCode.ProxyAuthenticationRequired:
                                    break;
                                case HttpStatusCode.RequestTimeout:
                                    break;
                                case HttpStatusCode.Conflict:
                                    break;
                                case HttpStatusCode.Gone:
                                    break;
                                case HttpStatusCode.LengthRequired:
                                    break;
                                case HttpStatusCode.PreconditionFailed:
                                    break;
                                case HttpStatusCode.RequestEntityTooLarge:
                                    break;
                                case HttpStatusCode.RequestUriTooLong:
                                    break;
                                case HttpStatusCode.UnsupportedMediaType:
                                    break;
                                case HttpStatusCode.RequestedRangeNotSatisfiable:
                                    break;
                                case HttpStatusCode.ExpectationFailed:
                                    break;
                                case HttpStatusCode.MisdirectedRequest:
                                    break;
                                case HttpStatusCode.UnprocessableEntity:
                                    break;
                                case HttpStatusCode.Locked:
                                    break;
                                case HttpStatusCode.FailedDependency:
                                    break;
                                case HttpStatusCode.UpgradeRequired:
                                    break;
                                case HttpStatusCode.PreconditionRequired:
                                    break;
                                case HttpStatusCode.TooManyRequests:
                                    break;
                                case HttpStatusCode.RequestHeaderFieldsTooLarge:
                                    break;
                                case HttpStatusCode.UnavailableForLegalReasons:
                                    break;
                                case HttpStatusCode.InternalServerError:
                                    break;
                                case HttpStatusCode.NotImplemented:
                                    break;
                                case HttpStatusCode.BadGateway:
                                    break;
                                case HttpStatusCode.ServiceUnavailable:
                                    break;
                                case HttpStatusCode.GatewayTimeout:
                                    break;
                                case HttpStatusCode.HttpVersionNotSupported:
                                    break;
                                case HttpStatusCode.VariantAlsoNegotiates:
                                    break;
                                case HttpStatusCode.InsufficientStorage:
                                    break;
                                case HttpStatusCode.LoopDetected:
                                    break;
                                case HttpStatusCode.NotExtended:
                                    break;
                                case HttpStatusCode.NetworkAuthenticationRequired:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                        if (!response.IsSuccessStatusCode)
                            {
                                OutputControl.FailedCrawlerUrls.TryAdd(address, 1);
                                _logger.LogError($"Request to {address} failed with status code {response.StatusCode}");

                                return await HandleNonSucess(response).ConfigureAwait(false);
                            }

                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return content;
                    }
                catch (TaskCanceledException ex)
                    {
                        _logger.LogWarning($"Request to {address} was cancelled", ex);
                        return string.Empty;
                    }
                catch (Exception ex)
                    {
                        _logger.LogError($"Unexpected error when making a request to {address}", ex);
                        return string.Empty;
                    }
            }

        #endregion




        #region Methods

        private async Task<string> HandleNonSucess(HttpResponseMessage response)
            {
                switch (response.StatusCode)
                    {
                        case HttpStatusCode.Redirect:
                            var redurl = response.Headers.Location;
                            return await GetHttpContentFromWebAsync(redurl.ToString());

                            break;
                        default:
                            return string.Empty;
                    }

                return string.Empty;
            }

        #endregion
    }