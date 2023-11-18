namespace KC.Apps.SpyderLib.Models;

public record struct PageContent
{
    #region Public Methods

    public PageContent(
        string url) : this()
        {
            this.Url = url;
            this.CacheFileName = string.Empty;
            this.Content = string.Empty;
            this.FromCache = false;
        }





  /// <summary>
  /// If page content is retrieved from cache this is the filename on disk
  /// </summary>
    public string CacheFileName { get; set; }

 /// <summary>
 /// Contents of page at the Url <see cref="Url"/>
 /// </summary>
    public string Content { get; set; }

    public bool FromCache { get; set; }

    /// <summary>
    ///     Gets or sets the Uniform Resource Locator (URL).
    /// </summary>
    /// <value>
    ///     The URL.
    /// </value>
    public string Url { get; init; }

    
    /// <summary>
    /// If an error occurred during the scraping of this url it will be populated here.
    /// </summary>
    public Exception Exception { get; set; }

    #endregion
}