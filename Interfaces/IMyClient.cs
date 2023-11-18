namespace KC.Apps.SpyderLib.Interfaces;

public interface IMyClient
{
    #region Public Methods

    Task<string> GetContentFromWebAsync(
        string address);





    Task<string> GetStringAsync(
        string requestUri);

    #endregion
}