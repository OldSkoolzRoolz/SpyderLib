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
public sealed class MyClient : IMyClient
{
    #region feeeldzzz

    private readonly HttpClient _client;

    #endregion






    /// <summary>
    ///     Encapsulated HttpClient. Encorporates reliable http operations including retry and error recovery using Polly API
    /// </summary>
    /// <param name="client"></param>
    public MyClient(IHttpClientFactory client)
    {
        if (client != null)
        {
            _client = client.CreateClient("SpyderClient");
        }
    }






    #region Public Methods

    public async Task<Stream> GetFileStreamFromWebAsync(string address)
    {
        return await GetFileStreamFromWebAsync(new(address), CancellationToken.None)
            .ConfigureAwait(false);
    }






    public async Task<string> GetPageContentFromWebAsync(string address)
    {
        return await GetPageContentFromWebAsync(new Uri(address)).ConfigureAwait(false);
    }

    #endregion






    #region Private Methods

    private async Task<Stream> GetFileStreamFromWebAsync(Uri address, CancellationToken token)
    {
        return await _client.GetStreamAsync(address, token).ConfigureAwait(false);
    }






    private async Task<string> GetPageContentFromWebAsync(Uri address)
    {
        try
        {
            var results = await _client.GetStringAsync(address).ConfigureAwait(false);
            return results;
        }
        catch (HttpRequestException) { }
        catch (TaskCanceledException) { }
        catch (SpyderException e)
        {
            Console.WriteLine(e);
        }

        return string.Empty;
    }

    #endregion
}