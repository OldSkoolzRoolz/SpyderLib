#region

#endregion

namespace KC.Apps.Interfaces;



public interface ISpyderWeb
{
    /// <summary>
    /// </summary>
    Task ProcessInputFileAsync();





    Task ScrapePageForHtmlTagAsync(string url);





    /// <summary>
    ///     Main spyder method starts crawling the given link according to options set
    /// </summary>
    /// <param name="startingLink"></param>
    Task StartSpyderAsync(string startingLink);
}