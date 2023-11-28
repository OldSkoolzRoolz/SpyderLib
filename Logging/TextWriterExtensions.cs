using JetBrains.Annotations;



namespace KC.Apps.SpyderLib.Logging;

public static class TextWriterExtensions
{
    private const string DEFAULT_BACKGROUND_COLOR = "\x1B[49m";
    private const string DEFAULT_FOREGROUND_COLOR = "\x1B[39m\x1B[22m";

    #region Public Methods

    public static void WriteWithColor(
        [NotNull] this TextWriter textWriter,
        [NotNull] string message,
        ConsoleColor? background,
        ConsoleColor? foreground)
        {
            ArgumentNullException.ThrowIfNull(argument: textWriter);
            ArgumentNullException.ThrowIfNull(argument: message);
            // Order:
            //   1. background color
            //   2. foreground color
            //   3. message
            //   4. reset foreground color
            //   5. reset background color
            var backgroundColor = background.HasValue ? GetBackgroundColorEscapeCode(color: background.Value) : null;
            var foregroundColor = foreground.HasValue ? GetForegroundColorEscapeCode(color: foreground.Value) : null;
            if (backgroundColor != null)
                {
                    textWriter.Write(value: backgroundColor);
                }

            if (foregroundColor != null)
                {
                    textWriter.Write(value: foregroundColor);
                }

            textWriter.Write(value: message);
            if (foregroundColor != null)
                {
                    textWriter.Write(value: DEFAULT_FOREGROUND_COLOR);
                }

            if (backgroundColor != null)
                {
                    textWriter.Write(value: DEFAULT_BACKGROUND_COLOR);
                }
        }

    #endregion

    #region Private Methods

    private static string GetBackgroundColorEscapeCode(
        ConsoleColor color)
        {
            return color switch
                {
                    ConsoleColor.Black => "\x1B[40m",
                    ConsoleColor.DarkRed => "\x1B[41m",
                    ConsoleColor.DarkGreen => "\x1B[42m",
                    ConsoleColor.DarkYellow => "\x1B[43m",
                    ConsoleColor.DarkBlue => "\x1B[44m",
                    ConsoleColor.DarkMagenta => "\x1B[45m",
                    ConsoleColor.DarkCyan => "\x1B[46m",
                    ConsoleColor.Gray => "\x1B[47m",
                    _ => DEFAULT_BACKGROUND_COLOR
                };
        }





    private static string GetForegroundColorEscapeCode(
        ConsoleColor color)
        {
            return color switch
                {
                    ConsoleColor.Black => "\x1B[30m",
                    ConsoleColor.DarkRed => "\x1B[31m",
                    ConsoleColor.DarkGreen => "\x1B[32m",
                    ConsoleColor.DarkYellow => "\x1B[33m",
                    ConsoleColor.DarkBlue => "\x1B[34m",
                    ConsoleColor.DarkMagenta => "\x1B[35m",
                    ConsoleColor.DarkCyan => "\x1B[36m",
                    ConsoleColor.Gray => "\x1B[37m",
                    ConsoleColor.Red => "\x1B[1m\x1B[31m",
                    ConsoleColor.Green => "\x1B[1m\x1B[32m",
                    ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
                    ConsoleColor.Blue => "\x1B[1m\x1B[34m",
                    ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
                    ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
                    ConsoleColor.White => "\x1B[1m\x1B[37m",
                    _ => DEFAULT_FOREGROUND_COLOR
                };
        }

    #endregion
}