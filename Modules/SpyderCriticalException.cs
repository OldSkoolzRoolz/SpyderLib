namespace KC.Apps.SpyderLib.Modules;

public class SpyderCriticalException : Exception
{
    public SpyderCriticalException() : base()
        {
        }





    public SpyderCriticalException(string message, Exception innerException) : base(message, innerException)
        {
        }

    public SpyderCriticalException(string message) : base(message)
        {
        }
}