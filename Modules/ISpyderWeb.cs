using SpyderLib.Models;



namespace SpyderLib.Modules;

public interface ISpyderWeb
{
    /// <summary>
    /// </summary>
    Task ProcessInputFileAsync();





    Task ScrapeUrlAsync(Uri url);


    Task<ConcurrentScrapedUrlCollection> ScrapePageForLinksAsync(string link);


    /// <summary>
    /// </summary>
    Task StartScrapingInputFileAsync();





    Task ScrapePageForHtmlTagAsync(string url);


    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    Task StartSpyderAsync(string startingLink);
}