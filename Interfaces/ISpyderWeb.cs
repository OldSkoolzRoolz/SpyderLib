namespace KC.Apps.SpyderLib.Interfaces;

public interface ISpyderWeb
{
    #region Public Methods

    void SearchLocalCacheForTags();


    Task StartScrapingInputFileAsync(CancellationToken token);





    Task StartSpyderAsync(
        string startingLink,
        CancellationToken token);

    #endregion
}