#region

using KC.Apps.SpyderLib.Models;

#endregion


namespace KC.Apps.SpyderLib.Modules;

[Serializable]
public class QueItem : Model
{
    #region Other Fields

    private string _progress;

    #endregion

    #region Public Methods

    public QueItem(
        string url)
        {
            this.Url = url;
        }





    public event EventHandler InterruptionRequested;

    public string Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }





    public void RequestInterruption()
        {
            var handler = this.InterruptionRequested;
            handler?.Invoke(this, EventArgs.Empty);
        }





    public string SavePath { get; set; }


    public string Url { get; set; }

    #endregion
}