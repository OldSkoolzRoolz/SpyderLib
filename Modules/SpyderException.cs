namespace KC.Apps.SpyderLib.Modules;

[Serializable]
public class SpyderException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="System.Exception" /> class.</summary>
    protected SpyderException() { }






    /// <summary>Initializes a new instance of the <see cref="System.Exception" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public SpyderException(
        string message) : base(message: message) { }






    /// <summary>
    ///     Initializes a new instance of the <see cref="System.Exception" /> class with a specified error message and a
    ///     reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception, or a null reference (
    ///     <see langword="Nothing" /> in Visual Basic) if no inner exception is specified.
    /// </param>
    public SpyderException(
        string message,
        Exception innerException) : base(message: message, innerException: innerException) { }
}