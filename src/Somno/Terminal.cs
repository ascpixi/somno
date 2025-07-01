using System;

namespace Somno;

internal static class Terminal
{
    public static void DisplayLine(string s, ConsoleColor fg, ConsoleColor bg)
    {
        Display(s, fg, bg);
        Console.WriteLine();
    }

    public static void Display(string s, ConsoleColor fg, ConsoleColor bg)
    {
        var oldFg = Console.ForegroundColor;
        var oldBg = Console.BackgroundColor;

        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;

        Console.Write(s);

        Console.ForegroundColor = oldFg;
        Console.BackgroundColor = oldBg;
    }

    public static void Header(string s, ConsoleColor fg, ConsoleColor bg)
    {
        var oldFg = Console.ForegroundColor;
        var oldBg = Console.BackgroundColor;

        Console.ForegroundColor = fg;
        Console.BackgroundColor = bg;

        Console.Write(s);
        for (int i = 0; i < Console.WindowWidth - s.Length; i++) {
            Console.Write(' ');
        }

        Console.ForegroundColor = oldFg;
        Console.BackgroundColor = oldBg;
    }

    public static void LogInfo(string s)
        => SanitizeAndLog("info", s, ConsoleColor.Black, ConsoleColor.Gray);

    public static void LogWarning(string s)
        => SanitizeAndLog("warn", s, ConsoleColor.Black, ConsoleColor.Yellow);

    public static void LogError(string s)
        => SanitizeAndLog("err!", s, ConsoleColor.Black, ConsoleColor.Red);

    static void SanitizeAndLog(string prefix, string s, ConsoleColor fg, ConsoleColor bg)
    {
        string[] lines = s.Split('\n');
        foreach (var line in lines) {
            Display(prefix, fg, bg);
            Console.Write(' ');
            Console.WriteLine(line);
        }
    }
}
