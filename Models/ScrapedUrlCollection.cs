using JetBrains.Annotations;

using KC.Apps.SpyderLib.Modules;



namespace KC.Apps.SpyderLib.Models;

public class ScrapedUrls(Uri baseUrl)
{
    #region feeeldzzz

    private readonly HashSet<Uri> _externalUrls = [];
    private readonly HashSet<Uri> _internalUrls = [];

    #endregion






    public Uri BaseUrl { get; } =
        baseUrl ?? throw new SpyderOptionsException("BaseUrl cannot be null. Check your options.");

    public IEnumerable<Uri> BaseUrls => _internalUrls;
    public IEnumerable<Uri> OtherUrls => _externalUrls;






    /// <summary>
    /// Adds a collection of URLs to the ScrapedUrlCollection
    /// </summary>
    /// <param name="urls">A collection of URLs</param>
    /// <exception cref="ArgumentNullException">Thrown if the input collection is null</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the input collection contains at least one null URL or an invalid URL
    /// </exception>
    public void AddRange([NotNull] IEnumerable<string> urls)
    {
        var filteredUrls = urls.Where(IsValidUrl).Select(url => new Uri(url)).ToList();

        foreach (var uri in filteredUrls)
        {
            AddUrl(uri);
        }
    }







    private void AddUrl([NotNull] Uri newUrl)
    {
        // Add the Uri object to the appropriate collection based on its kind
        _ = IsExternalUrl(newUrl)
            ? _externalUrls.Add(newUrl)
            : _internalUrls.Add(newUrl);
    }






    /// <summary>
    ///     Adds a URL to the internal or external URL collections based on its kind.
    /// </summary>
    /// <param name="url">The URL to be added. Must not be null.</param>
    public void AddUrl([NotNull] string url)
    {
        // Throw an exception if the URL is null
        ArgumentNullException.ThrowIfNull(url);
        if (IsValidUrl(url))
        {
            // Try to create a Uri object from the URL
            _ = Uri.TryCreate(url, UriKind.Absolute, out var uriResult);

            // Add the Uri object to the appropriate collection based on its kind
            _ = IsExternalUrl(uriResult)
                ? _externalUrls.Add(uriResult)
                : _internalUrls.Add(uriResult);
        }
    }






    /// <summary>
    ///     Determines whether the given URL is an external URL.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if the URL is external, false otherwise.</returns>
    private bool IsExternalUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        return this.BaseUrl.IsBaseOf(url);
    }






    /// <summary>
    ///     Determines whether the given URL is valid.
    /// </summary>
    /// <param name="newUrl">The URL to be validated.</param>
    /// <returns>True if the URL is valid, false otherwise.</returns>
    private static bool IsValidUrl(string newUrl)
    {
        return Uri.IsWellFormedUriString(newUrl, UriKind.Absolute) &&
               Uri.TryCreate(newUrl, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }







}