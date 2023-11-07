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





    public string CacheFileName { get; set; } = "Spyder-Index-Cache.json";
    public string Content { get; set; }
    public bool FromCache { get; set; }


    public string Url { get; init; }

    #endregion
}