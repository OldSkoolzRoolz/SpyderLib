namespace KC.Apps.SpyderLib.Modules;

public class SpyderCriticalException : Exception
{
    #region Public Methods

    public SpyderCriticalException() { }





    public SpyderCriticalException(string message, Exception innerException) : base(message: message,
        innerException: innerException) { }





    public SpyderCriticalException(string message) : base(message: message) { }

    #endregion
}