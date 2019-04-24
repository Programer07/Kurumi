using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Services.Database
{
    public static class KurumiPathConfig
    {
        public static string Root { get; private set; }
        public static string DbRoot { get; private set; }
        public static string Bin { get; private set; }
        public static string UserDatabase { get; private set; }
        public static string GuildDatabase { get; private set; }
        public static string CharacterDatabase { get; private set; }
        public static string Data { get; private set; }
        public static string MusicTemp { get; private set; }
        public static string Settings { get; private set; }
        public static string Quiz { get; private set; }
        public static string Graphics { get; private set; }
        public static string Temp { get; private set; }
        public static string YoutubeCache { get; private set; }
        public static string Separator { get; private set; }

        /// <summary>
        /// Configures the database paths based on the OS
        /// </summary>
        /// <param name="First">If true => missing directories will be created</param>
        /// <param name="ex"></param>
        /// <returns>True => success, False => failed</returns>
        public static bool TryConfigure(bool First, out Exception ex)
        {
            try
            {
                //Set variables
                Separator = @"\";
                if (IsLinux)
                {
                    Separator = "/";
                    Root = $"{Environment.GetEnvironmentVariable("HOME")}{Separator}Kurumi{Separator}";
                    Bin = null;
                }
                else
                {
                    Root = $"C:{Separator}Kurumi{Separator}";
                    Bin = Root + $"Data{Separator}Bin{Separator}";
                }
                Data = Root + $"Data{Separator}";
                DbRoot = Root + $"Database{Separator}";
                UserDatabase = DbRoot + $"UserDatabase{Separator}";
                GuildDatabase = DbRoot + $"ServerDatabase{Separator}";
                CharacterDatabase = DbRoot + $"CharacterDatabase{Separator}";
                MusicTemp = Data + $"Music{Separator}";
                Settings = Data + $"Settings{Separator}";
                Quiz = Data + $"Quiz{Separator}";
                Graphics = Data + $"Graphics{Separator}";
                Temp = Data + $"Temp{Separator}";
                YoutubeCache = Data + $"YoutubeCache{Separator}";

                if (First)
                {
                    //Print message
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("#Creating any missing directories...");
                    Console.ResetColor();
                    //Rebuild
                    Directory.CreateDirectory(Root);
                    Directory.CreateDirectory(DbRoot);
                    if (Bin != null) //FFMPEG and YoutubeDL don't need path on linux
                        Directory.CreateDirectory(Bin);
                    Directory.CreateDirectory(UserDatabase);
                    Directory.CreateDirectory(GuildDatabase);
                    Directory.CreateDirectory(CharacterDatabase);
                    Directory.CreateDirectory(Data);
                    Directory.CreateDirectory(MusicTemp);
                    Directory.CreateDirectory(Settings);
                    Directory.CreateDirectory(Quiz);
                    Directory.CreateDirectory(Graphics);
                    Directory.CreateDirectory(Temp);
                    Directory.CreateDirectory(YoutubeCache);
                    Directory.CreateDirectory(Settings + "Lang");
                }
                ex = null;
                return true;
            }
            catch (Exception exception)
            {
                ex = exception;
                return false;
            }
        }

        private static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
    }
}