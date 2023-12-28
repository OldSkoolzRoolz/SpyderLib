using CommunityToolkit.Diagnostics;



namespace KC.Apps.SpyderLib.Services;

public interface IMyClient
{
    #region Public Methods

    Task<Stream> GetFileStreamFromWebAsync(string address);


    Task<string> GetPageContentFromWebAsync(string address);

    #endregion
}



/// <summary>
///     Encapsulated HttpClient. Encorporates reliable http operations including retry and error recovery using Polly API
/// </summary>
/// <param name="client"></param>
public sealed class MyClient(HttpClient client) : IMyClient
{
    #region Public Methods

    public async Task<Stream> GetFileStreamFromWebAsync(string address)
        {
            return await GetFileStreamFromWebAsync(new(uriString: address), token: CancellationToken.None)
                .ConfigureAwait(false);
        }






    public async Task<string> GetPageContentFromWebAsync(string address)
        {
            return await GetPageContentFromWebAsync(new Uri(uriString: address)).ConfigureAwait(false);
        }

    #endregion






    #region Private Methods

    private async Task<Stream> GetFileStreamFromWebAsync(Uri address, CancellationToken token)
        {
            return await client.GetStreamAsync(requestUri: address, cancellationToken: token).ConfigureAwait(false);
        }






    private async Task<string> GetPageContentFromWebAsync(Uri address)
        {
            Guard.IsNotNull(value: client);

            try
                {
                    var results = await client.GetStringAsync(requestUri: address).ConfigureAwait(false);
                    return results;
                }
            catch (Exception e)
                {
                    Console.WriteLine(value: e);
                }

            return string.Empty;
        }

    #endregion
}