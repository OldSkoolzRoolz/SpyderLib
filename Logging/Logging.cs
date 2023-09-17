#region

using System.Diagnostics;
using System.Runtime.CompilerServices;

#endregion

namespace KC.Apps.Logging;



internal class Log
{
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
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", exception.Message, memberName, line, path);
            Debugger.Log(99, "Error", exception.Message);
            Console.ForegroundColor = front;
            Console.BackgroundColor = back;
        }





    internal static void Critical(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", message, memberName, line, path);
            Console.ForegroundColor = b42;
        }





    internal static void Debug(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", message, memberName, line, path);
            Console.ForegroundColor = b42;
        }





    internal static void Error(
        string message, [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", message, memberName, line, path);
            Console.ForegroundColor = b42;
        }





    internal static void Information(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", message, memberName, line, path);
            Console.ForegroundColor = b42;
        }





    internal static void Trace(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", message, memberName, line, path);
            Console.ForegroundColor = b42;
        }





    internal static void Warning(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string path = "")
        {
            var b42 = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("{0}::{1} : {3}. Line #{2}", message, memberName, line, path);
            Console.ForegroundColor = b42;
        }
}