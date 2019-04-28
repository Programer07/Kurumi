using Discord;
using Ionic.Zip;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using Kurumi.StartUp;
using MongoDB.Driver;
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
        private static MongoClient DatabaseClient { get; set; }
        public static IMongoDatabase Database { get; set; }

        public static readonly List<(IKurumiDatabase db, string type)> Databases = new List<(IKurumiDatabase db, string type)>()
        {
            (new GuildDatabase(), "guilds"),
            (new UserDatabase(), "users"),
            (new CharacterDatabase(), "characters")
        };

        public static void LoadDatabases()
        {
            Program.Bot.State = StartupState.DatabasesLoading;
            ConsoleHelper.WriteLine("Loading databases.", ConsoleColor.Blue);
            if (DatabaseClient == null)
            {
                string ConnectionString = "mongodb://";
                if (Config.MongoDBUsername != null)
                    ConnectionString += $"{Config.MongoDBUsername}:{Config.MongoDBPassword}@";
                ConnectionString += $"{Config.MongoDBIp}:{Config.MongoDBPort}";
                DatabaseClient = new MongoClient(ConnectionString);
                Database = DatabaseClient.GetDatabase(Config.MongoDBName);
            }

            var collections = Database.ListCollectionNames().ToList();
            bool r = false;
            for (int i = 0; i < collections.Count; i++)
            {
                if (collections[i].EndsWith("_Temp"))
                {
                    if (!r)
                        ConsoleHelper.WriteLine("Dropping unfinished saves...", ConsoleColor.Red);
                    r = true;
                    Database.DropCollection(collections[i]);
                }
            }

            for (int i = 0; i < Databases.Count; i++)
            {
                (IKurumiDatabase db, string type) = Databases[i];
                ConsoleHelper.WriteLine($"Loading {type}...", ConsoleColor.Cyan);
                try
                {
                    db.Load();
                }
                catch (Exception ex)
                {
                    Program.Crash($"loading {type}", ex);
                }
            }
            ConsoleHelper.WriteLine("Databases loaded.", ConsoleColor.Blue);
            Program.Bot.State = StartupState.DatabaseReady;
        }

        public static void SaveDatabases(bool Show)
        {
            if (!Show)
            {
                BackgroundSave();
                return;
            }

            ConsoleHelper.WriteLine("Saving databases.", ConsoleColor.Blue);
            for (int i = 0; i < Databases.Count; i++)
            {
                (IKurumiDatabase db, string type) = Databases[i];
                ConsoleHelper.WriteLine($"Saving {type}...", ConsoleColor.Cyan);
                try
                {
                    db.Save(Show);
                }
                catch (Exception ex)
                {
                    Program.Crash($"saving {type}", ex);
                }
            }
            ConsoleHelper.WriteLine("Databases saved.", ConsoleColor.Blue);
        }

        private static void BackgroundSave()
        {
            for (int i = 0; i < Databases.Count; i++)
            {
                Databases[i].db.Save(false);
            }
        }
    }
}