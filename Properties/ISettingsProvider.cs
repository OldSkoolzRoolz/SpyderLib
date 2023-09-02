namespace SpyderLib.Properties;

internal interface ISettingsProvider
{
    #region Methods

    T LoadSettings<T>(string fileName) where T : class, new();


    void SaveSettings(string fileName, object settings);

    #endregion
}