using System.Security.Policy;

using JetBrains.Annotations;



namespace KC.Apps.SpyderLib.Models;

public record struct PageContent
{
    public PageContent(Uri url) : this()
        {
            this.Address = url;
            this.CacheFileName = string.Empty;
            this.Content = string.Empty;
            this.FromCache = false;
        }






    #region Properteez

    /// <summary>
    ///     If page content is retrieved from cache this is the filename on disk
    /// </summary>
    public string CacheFileName { get; set; }

    /// <summary>
    ///     Contents of page at the Url <see cref="Url" />
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    ///     If an error occurred during the scraping of this url it will be populated here.
    /// </summary>
    [CanBeNull]
    public Exception Exception { get; set; }

    public bool FromCache { get; set; }

    /// <summary>
    ///     Gets or sets the Uniform Resource Locator (URL).
    /// </summary>
    /// <value>
    ///     The URL.
    /// </value>
    public Uri Address { get; init; }

    #endregion
}