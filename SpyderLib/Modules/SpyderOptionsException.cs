namespace KC.Apps.SpyderLib.Modules;

public class SpyderOptionsException : Exception
{
    #region Public Methods

    public SpyderOptionsException()
        {
        }





    public SpyderOptionsException(
        string message)
        : base(message)
        {
        }





    public SpyderOptionsException(
        string    message,
        Exception inner)
        : base(message, inner)
        {
        }

    #endregion
}