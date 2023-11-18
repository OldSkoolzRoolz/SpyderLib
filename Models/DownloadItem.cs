namespace KC.Apps.SpyderLib.Models;

/// <summary>
///     Download Item used for downloads
/// </summary>
public class DownloadItem
{
    internal DownloadItem(
        string link,
        string savePath, long actualBytes = 0, long expectedBytes = 0)
        {
            ArgumentNullException.ThrowIfNull(link);

            this.SavePath = savePath ?? throw new ArgumentNullException(nameof(savePath));
            this.ActualBytes = actualBytes;
            this.ExpectedBytes = expectedBytes;
            this.Link = link;
        }





    #region Properteez

    internal long ActualBytes { get; set; } = 0;
    internal long ExpectedBytes { get; set; } = 0;
    internal string Link { get; }
    internal string SavePath { get; }

    #endregion
}