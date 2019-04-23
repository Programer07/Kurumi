using Discord;
using Ionic.Zip;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Modules.Games.Duel.Database;
using Kurumi.Services.Database.Databases;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Database
{
    public class DatabaseManager
    {
        public static readonly List<(IKurumiDatabase db, string type)> Databases = new List<(IKurumiDatabase, string)>()
        {
            (new GuildConfigDatabase(), "guild configs"),
            (new GlobalUserDatabase(), "global users"),
            (new GuildUserDatabase(), "guild users"),
            (new AfkMessageDatabase(), "afk messages"),
            (new ModDatabase(), "warnings"),
            (new RewardDatabase(), "rewards"),
            (new CharacterDatabase(), "characters")
        };
        private static byte LastSaved = 32;

        public static void Save()
        {
            for (int i = 0; i < Databases.Count; i++)
            {
                var db = Databases[i].db;
                db.Save(false);
            }

            if (DateTime.Now.Day != LastSaved && DateTime.Now.Hour < 3)
            {
                if (Config.BackupDB)
                    Backup();
                LastSaved = (byte)DateTime.Now.Day;
            }
        }

        private static Task Backup()
        {
            return Task.Run(() => 
            {
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    Utilities.Log(new LogMessage(LogSeverity.Info, "DatabaseManager", "Starting database backup!"));
                    var BackupPath = $"{KurumiPathConfig.Root}Backup{KurumiPathConfig.Separator}{DateTime.Now.ToShortDateString().Replace("/", "-")}{KurumiPathConfig.Separator}";
                    Directory.CreateDirectory(BackupPath);
                    var DatabaseDirectories = Directory.GetDirectories(KurumiPathConfig.DbRoot);
                    for (int i = 0; i < DatabaseDirectories.Length; i++)
                    {
                        DirectoryInfo dir = new DirectoryInfo(DatabaseDirectories[i]);
                        if (dir.Name == "CharacterDatabase") //Character database cannot be compressed because of the names
                        {
                            DirectoryExtensions.Copy(DatabaseDirectories[i], $"{BackupPath}{dir.Name}", true);
                            continue;
                        }

                        ZipFile file = new ZipFile
                        {
                            CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed,
                            Comment = "Kurumi database backup. Date: " + DateTime.Now,
                            UseZip64WhenSaving = Zip64Option.AsNecessary,
                            AlternateEncodingUsage = ZipOption.AsNecessary,
                            TempFileFolder = KurumiPathConfig.Temp
                        };
                        file.AddDirectory(dir.FullName);
                        file.Save($"{BackupPath}{dir.Name}.zip");
                    }
                    sw.Stop();
                    Utilities.Log(new LogMessage(LogSeverity.Info, "DatabaseManager", $"Database backup was successful. Took: {sw.ElapsedMilliseconds} ms"));
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    Utilities.Log(new LogMessage(LogSeverity.Error, "DatabaseManager", "Database backup failed!\n", ex));
                }
                return Task.CompletedTask;
            });
        }
    }
}