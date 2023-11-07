namespace KC.Apps.SpyderLib.Interfaces;

public interface ISpyderWeb
{
    #region Public Methods

    Task DownloadVideoTagsFromUrl(
        string url);





    /// <summary>
    /// </summary>
    Task StartScrapingInputFileAsync(CancellationToken token);





    Task StartSpyderAsync(
        string            startingLink,
        CancellationToken token);

    #endregion
}