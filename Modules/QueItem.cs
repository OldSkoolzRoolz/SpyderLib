#region

using KC.Apps.SpyderLib.Models;

#endregion

namespace KC.Apps.SpyderLib.Modules;

[Serializable]
public class QueItem : Model
{
    private string _progress;

    #region Public Methods

    public QueItem(
        string url)
        {
            this.Url = url;
        }





    public string Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

    public string SavePath { get; set; }
    public string Url { get; set; }

    #endregion
}