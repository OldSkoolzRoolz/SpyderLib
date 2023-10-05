#region

#endregion




#region

using KC.Apps.SpyderLib.Models;

#endregion




namespace KC.Apps.Interfaces;




internal interface IDownloadControl
    {
        #region Methods

        void AddDownloadItem(DownloadItem item);


        void SetInputComplete();

        #endregion
    }