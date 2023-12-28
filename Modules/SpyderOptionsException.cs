namespace KC.Apps.SpyderLib.Modules;

public class SpyderOptionsException : Exception
{
    public SpyderOptionsException() { }






    public SpyderOptionsException(
        string message)
        : base(message: message) { }






    public SpyderOptionsException(
        string message,
        Exception inner)
        : base(message: message, innerException: inner) { }
}