using KC.Apps.SpyderLib.Models;



namespace KC.Apps.SpyderLib.Modules;

/// <inheritdoc />
[Serializable]
internal sealed class QueItem(Uri url) : Model
{
    #region feeeldzzz

    private string _progress;

    #endregion






    #region Properteez

    public string Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }



    public string SavePath { get; set; }
    public Uri Url { get; set; } = url;

    #endregion
}