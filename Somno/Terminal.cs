using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno
{
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
        {
            Display("info", ConsoleColor.Black, ConsoleColor.Gray);
            Console.Write(' ');
            Console.WriteLine(s);
        }

        public static void LogWarning(string s)
        {
            Display("warn", ConsoleColor.Black, ConsoleColor.Yellow);
            Console.Write(' ');
            Console.WriteLine(s);
        }

        public static void LogError(string s)
        {
            Display("err!", ConsoleColor.Black, ConsoleColor.Red);
            Console.Write(' ');
            Console.WriteLine(s);
        }
    }
}
