using Kurumi.Common;
using Kurumi.Properties;
using Kurumi.Services.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Kurumi.StartUp
{
    public class ConsoleMode
    {
        private static readonly List<(string cmd, string help, Action<string> Handler)> Commands = new List<(string cmd, string help, Action<string> Handler)>()
        {
            ("quit", "Close the application", Quit),
            ("help", "List of commands", PrintHelp),
            ("extract", "Extract a file from Kurumi. (Valid: config, items, kurumidata, commands, en)", Extract)
        };

        private static bool Waiting = true;
        private static bool PathConfigured = false;
        public static void CMain()
        {
            Console.WriteLine("Successfuly started in console mode!\n");
            PrintHelp();
            Console.WriteLine();
            while (Waiting)
                CommandHandler(Console.ReadLine());
        }

        public static void CommandHandler(string Input)
        {
            if (string.IsNullOrWhiteSpace(Input))
                return;
            Input = Input.ToLower().Trim();

            int index = Input.IndexOf(' ');
            var args = string.Empty;
            string command;
            if (index == -1)
                command = Input;
            else
            {
                command = Input.Substring(0, index);
                args = Input.Substring(index + 1, Input.Length - index - 1);
            }
 
            for (int i = 0; i < Commands.Count; i++)
            {
                var (cmd, help, Handler) = Commands[i];
                if (cmd == command)
                    Handler.Invoke(args);
            }

            Console.WriteLine();
        }

        #region Commands
        private static void Quit(string s) => Waiting = false;
        private static void Extract(string s)
        {
            if (!ConfigurePath())
                return;

            Console.WriteLine("Extracting file...");
            byte[] FileContent = null;
            string Path = null;
            if (s == "commands")
            {
                FileContent = Resources.Commands;
                Path = $"{KurumiPathConfig.Settings}Commands.json";
            }
            else if (s == "config")
            {
                FileContent = Resources.Config;
                Path = $"{KurumiPathConfig.Settings}Config.json";
            }
            else if (s == "en")
            {
                FileContent = Resources.en;
                Path = $"{KurumiPathConfig.Settings}Lang{KurumiPathConfig.Separator}en.json";
            }
            else if (s == "items")
            {
                FileContent = Resources.Items;
                Path = $"{KurumiPathConfig.Data}Items.json";
            }
            else if (s == "kurumidata")
            {
                FileContent = Resources.KurumiData;
                Path = $"{KurumiPathConfig.Data}KurumiData.json";
            }

            if(FileContent == null)
                Console.WriteLine("File not found!");
            else
            {
                Console.WriteLine("File found, extracting...");
                File.WriteAllBytes(Path, FileContent);
                Console.WriteLine($"File extracted to: {Path}");
            }
        }
        private static void PrintHelp(string s = null)
        {
            Console.WriteLine("Available Commands:");
            for (int i = 0; i < Commands.Count; i++)
            {
                var (cmd, help, _) = Commands[i];
                Console.WriteLine($"   {cmd} - {help}");
            }
        }
        #endregion Commands

        private static bool ConfigurePath()
        {
            if (PathConfigured)
                return true;
            KurumiPathConfig.TryConfigure(true, out Exception ex);
            if (ex != null)
            {
                Console.WriteLine("Failed to configure paths! Exception:\n" + ex);
                return false;
            }
            return PathConfigured = true;
        }
    }
}