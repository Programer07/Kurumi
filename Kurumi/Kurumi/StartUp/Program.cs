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
        public static bool Exiting { get; set; }


        static async Task Main(string[] args)
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
            ConsoleHelper.WriteLine("#======================================#", ConsoleColor.Green);
            ConsoleHelper.WriteLine("#               Welcome!               #", ConsoleColor.Green);
            ConsoleHelper.WriteLine("#======================================#", ConsoleColor.Green);

            //Print os
            ConsoleHelper.WriteLine($"Starting Kurumi, running on {Environment.OSVersion.ToString()}", ConsoleColor.Yellow);
            Thread.Sleep(100);

            //Configure database paths
            ConsoleHelper.WriteLine("Configuring database path...", ConsoleColor.Cyan);
            if (!KurumiPathConfig.TryConfigure(out Exception error))
                Crash("configuring database path", error);
            ConsoleHelper.WriteLine("Successful configuration.", ConsoleColor.Green);

            //Load config
            ConsoleHelper.WriteLine("Loading config...", ConsoleColor.Cyan);
            if (!Config.TryLoad(out error))
                Crash("loading config file", error);
            ConsoleHelper.WriteLine("Successfuly loaded config.", ConsoleColor.Green);

            //Load languages
            ConsoleHelper.WriteLine("Loading languages...", ConsoleColor.Cyan);
            if (!Language.LoadLanguages(out error))
                Crash("loading languages", error);
            ConsoleHelper.WriteLine("Successfuly loaded languages.", ConsoleColor.Green);

            //Load commands
            ConsoleHelper.WriteLine("Loading commands...", ConsoleColor.Cyan);
            if (!CommandData.LoadCommands(out error))
                Crash("loading commands", error);
            ConsoleHelper.WriteLine("Successfuly loaded commands.", ConsoleColor.Green);

            //Load databases
            DatabaseManager.LoadDatabases();

            //Setup events
            AppDomain.CurrentDomain.ProcessExit += (o, args) =>
            {
                if (!ExitHandled)
                    ConsoleHelper.WriteLine("WARNING! DO NOT CLOSE KURUMI! USE QUIT! Saving what can be saved...", ConsoleColor.Red);
                Quit().GetAwaiter().GetResult();
            };
            AppDomain.CurrentDomain.UnhandledException += (o, ex) =>
            {
                var Exception = ex.ExceptionObject as Exception;

                string Path = $"{KurumiPathConfig.Root}err_{new Random().Next(0, 100000)}.txt";
                Utilities.Log(new LogMessage(LogSeverity.Critical, "AppDomain", $"Critical error! Report was saved to: {Path}. Terminating in 5 seconds!"));
                File.WriteAllText(Path, Exception.ToString());

                Thread.Sleep(5000);
                Environment.Exit(Marshal.GetHRForException(Exception));
            };

            Bot.State = StartupState.EventsLoading;
            EventScheduler.Current.Add(new Event(null, EventType.ESSave, -1, DateTime.Now + new TimeSpan(0, 20, 0), new TimeSpan(0, 20, 0), null, false, "System Event", false, true));
            EventScheduler.Current.Add(new Event(null, EventType.DBSave, -1, DateTime.Now + new TimeSpan(0, 30, 0), new TimeSpan(0, 30, 0), null, false, "System Event", false, true));
            EventScheduler.Current.Add(new Event(null, EventType.StatusUpdate, -1, new DateTime(1970, 1, 1), new TimeSpan(0, 2, 0), null, false, "System Event", false, true));
            Bot.State = StartupState.EventsLoaded;

            //Print load time
            sw.Stop();
            ConsoleHelper.WriteLine("Load took: " + sw.ElapsedMilliseconds + "ms", ConsoleColor.DarkGray);

            //LINK START
            ConsoleHelper.WriteLine("# -> # -> # LINK START! # <- # <- #", ConsoleColor.Yellow);
            Console.ResetColor();

            await Bot.Start();
        }
        public static void Crash(string op, Exception ex, int exitCode = 0)
        {
            lock (Utilities.WriteLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while {op}:\n{ex}");
                Console.ReadLine();
                ExitHandled = true; //If it crashed trying to save is not the best idea, skip it.
                Environment.Exit(exitCode);
            }
        }
        public static async Task Quit()
        {
            if (Exiting) //If its already exiting wait for it to finish then return
            {
                while (Exiting)
                {
                    await Task.Delay(1000);
                }
                return;
            }
            if (ExitHandled)
                return;
            Exiting = true;

            ConsoleHelper.WriteLine("Shutting down!", ConsoleColor.Yellow);
            EventScheduler.Current.Stop();
            Thread.Sleep(100);

            //Stop music streams
            if (Bot.State >= StartupState.Ready)
            {
                try
                {
                    ConsoleHelper.WriteLine("Stoping music streams...", ConsoleColor.Yellow);
                    foreach (var player in Music.MusicPlayers.Values)
                        player.Leave();
                    while (Music.MusicPlayers.Count != 0) //Wait for all to be stopped
                        await Task.Delay(10);
                    ConsoleHelper.WriteLine("Music streams stopped.", ConsoleColor.Green);
                }
                catch (Exception) { ConsoleHelper.WriteLine("Failed to stop some music streams!", ConsoleColor.Red); }
            }

            //Leave discord
            if (Bot.State >= StartupState.Ready)
            {
                try
                {
                    ConsoleHelper.WriteLine("Leaving discord...", ConsoleColor.Yellow);
                    await Bot.Shutdown();
                    ConsoleHelper.WriteLine("Disconnected.", ConsoleColor.Green);
                }
                catch (Exception) { ConsoleHelper.WriteLine("Failed to leave discord!", ConsoleColor.Red); }
            }

            //Save databases
            if (Bot.State >= StartupState.DatabaseReady)
            {
                try
                {
                    DatabaseManager.SaveDatabases(true);
                }
                catch (Exception) { ConsoleHelper.WriteLine("Failed to save databases!", ConsoleColor.Red); }
            }

            //Save events
            if (Bot.State >= StartupState.EventsLoaded)
            {
                ConsoleHelper.WriteLine("Saving events...", ConsoleColor.Yellow);
                try
                {
                    EventScheduler.Current.Save();
                    EventScheduler.Current.Dispose();
                    ConsoleHelper.WriteLine("Saved events.", ConsoleColor.Green);
                }
                catch (Exception) { ConsoleHelper.WriteLine("Failed to save events!", ConsoleColor.Red); }
            }

            Thread.Sleep(200);
            ConsoleHelper.WriteLine("Exiting...", ConsoleColor.Red);
            Exiting = false;
            ExitHandled = true;
        }
    }
}