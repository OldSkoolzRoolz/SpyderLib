#region

#endregion


namespace KC.Apps.SpyderLib.Interfaces;

public interface ISpyderWeb
{
    /// <summary>
    /// </summary>
    Task StartScrapingInputFileAsync();





    Task StartSpyderAsync(string startingLink, CancellationToken token);
}