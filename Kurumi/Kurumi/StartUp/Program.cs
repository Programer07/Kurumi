using Discord;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Modules.Music;
using Kurumi.Services;
using Kurumi.Services.Database;
using Kurumi.Services.Permission;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kurumi.StartUp
{
    public class Program
    {
        public static KurumiBot Bot { get; private set; }
        public static bool ExitHandled { get; set; }


        static void Main(string[] args)
        {
            /*int Count = 0;
            string[] Files = Directory.GetFiles(@"C:\Kurumi\Kurumi\Kurumi", "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                Count += File.ReadAllLines(Files[i]).Length;
            }
            Console.WriteLine(Count);*/

            Console.Title = "Kurumi";
            if (args.Length != 0 && args[0] == "--console")
            {
                ConsoleMode.CMain();
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();

            Bot = new KurumiBot();

            //Print welcome message
            ConsoleWriteLine("#======================================#", ConsoleColor.Green);
            ConsoleWriteLine("#               Welcome!               #", ConsoleColor.Green);
            ConsoleWriteLine("#======================================#", ConsoleColor.Green);

            //Print os
            ConsoleWriteLine($"#Starting Kurumi, running on {Environment.OSVersion.ToString()}", ConsoleColor.Yellow);
            Thread.Sleep(100);

            //Configure database paths
            ConsoleWriteLine("#Configuring database path...", ConsoleColor.Cyan);
            if (!KurumiPathConfig.TryConfigure(false, out Exception error))
                Crash("configuring database path", error);
            ConsoleWriteLine("#Successful configuration.", ConsoleColor.Green);

            //Load config
            ConsoleWriteLine("#Loading config...", ConsoleColor.Cyan);
            if (!Config.TryLoad(out error))
                Crash("loading config file", error);
            ConsoleWriteLine("#Successfuly loaded config.", ConsoleColor.Green);

            //Load languages
            ConsoleWriteLine("#Loading languages...", ConsoleColor.Cyan);
            if (!Language.LoadLanguages(out error))
                Crash("loading languages", error);
            ConsoleWriteLine("#Successfuly loaded languages.", ConsoleColor.Green);

            //Load commands
            ConsoleWriteLine("#Loading commands...", ConsoleColor.Cyan);
            if (!CommandData.LoadCommands(out error))
                Crash("loading commands", error);
            ConsoleWriteLine("#Successfuly loaded commands.", ConsoleColor.Green);

            //Load databases
            ConsoleWriteLine("#Loading databases...", ConsoleColor.Blue);
            for (int i = 0; i < DatabaseManager.Databases.Count; i++)
            {
                (IKurumiDatabase db, string type) = DatabaseManager.Databases[i];
                ConsoleWriteLine("#Loading " + type + "...", ConsoleColor.Cyan);
                try
                {
                    db.Load();
                }
                catch(Exception ex)
                {
                    Crash("loading " + type.ToLower(), ex, 0);
                }
            }
            ConsoleWriteLine("#Databases loaded.", ConsoleColor.Blue);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Exit); //Setup exit
            AppDomain.CurrentDomain.UnhandledException += (o, ex) =>
            {
                var Exception = (ex.ExceptionObject as Exception);

                string Path = $"{KurumiPathConfig.Root}err_{new Random().Next(0, 100000)}.txt";
                Utilities.Log(new LogMessage(LogSeverity.Critical, "AppDomain", $"Critical error! Report was saved to: {Path}. Terminating in 5 seconds!"));
                File.WriteAllText(Path, Exception.ToString());

                Thread.Sleep(5000);
                Environment.Exit(Marshal.GetHRForException(Exception));
            };

            EventScheduler.Current.Add(new Event(null, EventType.ESSave, -1, DateTime.Now + new TimeSpan(0, 20, 0), new TimeSpan(0, 20, 0), null, false, "System Event", false, true));
            EventScheduler.Current.Add(new Event(null, EventType.DBSave, -1, DateTime.Now + new TimeSpan(0, 30, 0), new TimeSpan(0, 30, 0), null, false, "System Event", false, true));
            EventScheduler.Current.Add(new Event(null, EventType.StatusUpdate, -1, new DateTime(1970, 1, 1), new TimeSpan(0, 2, 0), null, false, "System Event", false, true));

            //Print load time
            sw.Stop();
            ConsoleWriteLine("#Load took: " + sw.ElapsedMilliseconds + "ms", ConsoleColor.DarkGray);

            //LINK START
            ConsoleWriteLine("# -> # -> # LINK START! # <- # <- #", ConsoleColor.Yellow);
            Console.ResetColor();

            Bot.Start().GetAwaiter().GetResult();
        }

        public static void ConsoleWriteLine(string Text, ConsoleColor Color)
        {
            lock (Utilities.WriteLock)
            {
                ConsoleColor o = Console.ForegroundColor;
                Console.ForegroundColor = Color;
                Console.WriteLine(Text);
                Console.ForegroundColor = o;
            }
        }
        protected static Task Crash(string op, Exception ex, int exitCode = 0)
        {
            lock (Utilities.WriteLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while {op}:\n{ex}");
                Console.ReadLine();
                Environment.Exit(exitCode);
            }
            return Task.CompletedTask;
        }
        public static void Exit(object sender, EventArgs e)
        {
            if (ExitHandled)
                return;

            ConsoleWriteLine("#Shutting down!", ConsoleColor.Yellow);
            EventScheduler.Current.Stop();
            Thread.Sleep(100);

            //Stop music streams
            try
            {
                ConsoleWriteLine("#Stoping music streams...", ConsoleColor.Yellow);
                foreach (var player in Music.MusicPlayers.Values)
                    player.Leave();
                Thread.Sleep(150); //Leaving is asynchronous, wait 150 ms to make sure it finished.
                ConsoleWriteLine("#Music streams stopped.", ConsoleColor.Green);
            }
            catch(Exception)
            {
                ConsoleWriteLine("#Failed to stop some music streams!", ConsoleColor.Red);
            }

            //Leave discord
            try
            {
                ConsoleWriteLine("#Leaving discord...", ConsoleColor.Yellow);
                Bot.Shutdown();
                ConsoleWriteLine("#Disconnected.", ConsoleColor.Green);
            }
            catch (Exception)
            {
                ConsoleWriteLine("#Failed to leave discord!", ConsoleColor.Red);
            }

            //Save databases
            ConsoleWriteLine("#Saving databases...", ConsoleColor.Blue);
            for (int i = 0; i < DatabaseManager.Databases.Count; i++)
            {
                (IKurumiDatabase db, string type) = DatabaseManager.Databases[i];
                ConsoleWriteLine("#Saving " + type + "...", ConsoleColor.Cyan);
                try
                {
                    db.Save(true);
                }
                catch (Exception)
                {
                    ConsoleWriteLine("#Failed to save " + type + "!", ConsoleColor.Red);
                }
            }
            ConsoleWriteLine("#Databases saved.", ConsoleColor.Blue);

            //Save events
            ConsoleWriteLine("#Saving events...", ConsoleColor.Yellow);
            try
            {
                EventScheduler.Current.Save();
                EventScheduler.Current.Dispose();
                ConsoleWriteLine("#Saved events.", ConsoleColor.Green);
            }
            catch (Exception)
            {
                ConsoleWriteLine("#Failed to save events!", ConsoleColor.Red);
            }

            Thread.Sleep(200);
            ConsoleWriteLine("#Exiting...", ConsoleColor.Red);
        }
    }
}