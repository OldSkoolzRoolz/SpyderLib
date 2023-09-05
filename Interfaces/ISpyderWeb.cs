namespace KC.Apps.SpyderLib.Modules;

public interface ISpyderWeb
{
    /// <summary>
    /// </summary>
    Task ProcessInputFileAsync();





    Task ScrapePageForHtmlTagAsync(string url);


    Task<ConcurrentScrapedUrlCollection> ScrapePageForLinksAsync(string link);


    Task ScrapeUrlAsync(Uri url);





    /// <summary>
    /// </summary>
    Task StartScrapingInputFileAsync();





    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    Task StartSpyderAsync(string startingLink);
}