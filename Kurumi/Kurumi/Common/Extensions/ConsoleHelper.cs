using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Common.Extensions
{
    public static class ConsoleHelper
    {
        public static void ClearCurrentLine()
        {
            lock (Utilities.WriteLock)
            {
                int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currentLineCursor);
            }
        }
        public static void WriteLine(string Text, ConsoleColor Color)
        {
            lock (Utilities.WriteLock)
            {
                ConsoleColor o = Console.ForegroundColor;
                Console.ForegroundColor = Color;
                Console.WriteLine(Text);
                Console.ForegroundColor = o;
            }
        }
        public static void Write(string Text, ConsoleColor Color)
        {
            lock (Utilities.WriteLock)
            {
                ConsoleColor o = Console.ForegroundColor;
                Console.ForegroundColor = Color;
                Console.Write("\r" + Text);
                Console.ForegroundColor = o;
            }
        }
    }
}