using KC.Apps.SpyderLib.Models;



namespace KC.Apps.SpyderLib.Modules;

/// <inheritdoc />
[Serializable]
internal  class QueItem(Uri url) : Model
{
    private string _progress;






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