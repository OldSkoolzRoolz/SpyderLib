#region

#endregion




namespace KC.Apps.SpyderLib.Modules;




public interface ISpyderWeb
    {
        #region Methods

        /// <summary>
        ///     Generic method for processing tasks with throttling
        /// </summary>
        /// <param name="tasks"></param>
        Task ProcessTasksAsync(IEnumerable<Task> tasks);





        /// <summary>
        /// </summary>
        /// <exception cref="@_0"></exception>
        Task StartScrapingInputFileAsync();





        Task StartSpyderAsync(string startingLink);

        #endregion
    }