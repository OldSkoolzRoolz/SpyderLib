using JetBrains.Annotations;

using KC.Apps.SpyderLib.Properties;



namespace KC.Apps.SpyderLib.Models;

public class ScrapedUrls(string startingHost)
{
    #region feeeldzzz

    private readonly HashSet<Uri> _externalUrls = [];
    private readonly HashSet<Uri> _internalUrls = [];
    private readonly SpyderOptions _options = AppContext.GetData("options") as SpyderOptions;

    #endregion






    public Uri BaseUrl { get; } = new(startingHost);
    public IEnumerable<Uri> BaseUrls => _internalUrls;
    public IEnumerable<Uri> OtherUrls => _externalUrls;
    public IEnumerable<Uri> AllUrls => _internalUrls.Union(_externalUrls);
    public IEnumerable<string> AllUrlz => this.AllUrls.Select(x => x.OriginalString);
    public int Count => _internalUrls.Count + _externalUrls.Count;






    public bool Any()
        {
            return this.AllUrls.Any();
        }






    /// <summary>
    ///     Adds a collection of URLs to the ScrapedUrlCollection
    /// </summary>
    /// <param name="urls">A collection of URLs</param>
    /// <exception cref="ArgumentNullException">Thrown if the input collection is null</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown if the input collection contains at least one null URL or an invalid URL
    /// </exception>
    public void AddRange([NotNull] IEnumerable<string> urls)
        {
            ArgumentNullException.ThrowIfNull(urls);

            foreach (var uri in urls)
                {
                    AddUrl(new Uri(uri));
                }
        }






    /// <summary>
    ///     Adds a collection of URLs to the ScrapedUrlCollection
    /// </summary>
    /// <param name="urls">A collection of URLs</param>
    /// <exception cref="ArgumentNullException">Thrown if the input collection is null</exception>
    public void AddRange([NotNull] IEnumerable<Uri> urls)
        {
            ArgumentNullException.ThrowIfNull(urls, nameof(urls));

            foreach (var uri in urls)
                {
                    AddUrl(uri);
                }
        }






    /// <summary>
    ///     Adds a URL to the internal or external URL collections based on its kind.
    /// </summary>
    /// <param name="newUrl">
    ///     The URL to be added. Must not be null and must be a valid absolute URI.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the input parameter is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown if the input parameter is not a valid absolute URI.
    /// </exception>
    public void AddUrl([NotNull] Uri newUrl)
        {
            // Throw an exception if the URL is null
            ArgumentNullException.ThrowIfNull(newUrl);
            // Validate the input URL
            if (!IsValidUrl(newUrl.OriginalString))
                {
                    return;
                }

            // Add the Uri object to the appropriate collection based on its kind
            if (IsExternalUrl(newUrl))
                {
                    _ = _externalUrls.Add(newUrl);
                }
            else
                {
                    _ = _internalUrls.Add(newUrl);
                }
        }






    /// <summary>
    ///     Adds a URL to the internal or external URL collections based on its kind.
    /// </summary>
    /// <param name="url">The URL to be added. Must not be null.</param>
    public void AddUrl([NotNull] string address)
        {
            // Throw an exception if the URL is null
            ArgumentNullException.ThrowIfNull(address);




            if (IsValidUrl(address) && !CheckStringForSubstring(address, _options.LinkPatternExclusions))
                {
                    // Try to create a Uri object from the address
                    _ = Uri.TryCreate(address, UriKind.Absolute, out var uriResult);

                    // Add the Uri object to the appropriate collection based on its kind
                    AddUrl(uriResult);
                }
        }



private bool CheckStringForSubstring(string url, string[] substring)
    {
        foreach (var s in substring)
            {
                if (url.Contains(s))
                    {
                        return true;
                    }
            }
        return false;
    }






    /// <summary>
    ///     Determines whether the given URL is an external URL.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if the URL is external, false otherwise.</returns>
    private bool IsExternalUrl(Uri url)
        {
            ArgumentNullException.ThrowIfNull(url);
            return !this.BaseUrl.IsBaseOf(url);
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










    private IEnumerable<string> FilterPatternsFromUrls(IEnumerable<string> urls)
        {
            var patterns = _options.LinkPatternExclusions;

            return patterns is null
                ? urls
                :
                // Returns the urls that do not contain any of the patterns.
                urls.Where(url =>
                    !patterns.Any(pattern =>
                        url.Contains(pattern, StringComparison.CurrentCultureIgnoreCase)));
        }

    internal ScrapedUrls RemoveVisitedUrls(ScrapedUrls visitedUrls)
    {
        _internalUrls.ExceptWith(visitedUrls._internalUrls);
        _externalUrls.ExceptWith(visitedUrls._externalUrls);
        return this;

    }




internal void Remove(Uri address)
{
  
        _internalUrls.Remove(address);

        _externalUrls.Remove(address);
    
}




}