#region

using KC.Apps.SpyderLib.Models;

#endregion

namespace KC.Apps.SpyderLib.Modules;

[Serializable]
public class QueItem(Uri url) : Model
{
    private string _progress;

    #region Public Methods

    public string Progress
        {
            get => _progress;
            set => SetProperty(field: ref _progress, value: value);
        }

    public string SavePath { get; set; }
    public Uri Url { get; set; } = url;

    #endregion
}