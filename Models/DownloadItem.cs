namespace KC.Apps.SpyderLib.Models;




/// <summary>
/// </summary>
public class DownloadItem
    {
        internal DownloadItem(string link, string savepath)
            {
                ArgumentNullException.ThrowIfNull(link);
                if (savepath == null)
                    {
                        throw new ArgumentNullException(nameof(savepath));
                    }

                this.SavePath = savepath;
                this.Link = link;
            }





        #region Prop

        internal long ActualBytes { get; set; }
        internal long? ExpectedBytes { get; set; }
        internal string Link { get; set; }
        internal string SavePath { get; set; }

        #endregion
    }