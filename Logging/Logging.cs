using System.Diagnostics;
using System.Runtime.CompilerServices;



// ReSharper disable LocalizableElement
namespace KC.Apps.SpyderLib.Logging;

public static class Log
{
    #region Private Methods

    /// <summary>
    ///     Method captures calling information and line number and saves to log.
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="memberName"></param>
    /// <param name="line"></param>
    /// <param name="path"></param>
    internal static void AndContinue(
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var back = Console.BackgroundColor;
            var front = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"<INFO> {exception.Message}::{memberName} : {path}. Line #{line}");
            Debugger.Log(99, "Error", exception.Message);
            Console.ForegroundColor = front;
            Console.BackgroundColor = back;
        }






    internal static void Debug(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"<DEBUG> {message}::{memberName} : {path}. Line #{line}");
            Console.ForegroundColor = b42;
        }






    internal static void LogCriticalMessage(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"<CRITICAL> {message}::{memberName} : {filePath}. Line #{lineNumber}");
            Console.ForegroundColor = previousColor;
        }

    #endregion
}