namespace KC.Apps.SpyderLib.Interfaces;

public interface ISpyderWeb
{
    #region Public Methods

    Task DownloadVideoTagsFromUrl(
        string url);





    Task StartScrapingInputFileAsync(CancellationToken token);





    Task StartSpyderAsync(
        string startingLink,
        CancellationToken token);

    #endregion
}