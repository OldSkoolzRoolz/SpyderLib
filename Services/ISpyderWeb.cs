namespace KC.Apps.SpyderLib.Services;

public interface ISpyderWeb
{
    #region Public Methods

    Task StartSpyderAsync(
        string startingLink,
        CancellationToken token);

    #endregion
}