using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Properties;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            ("extract", "Extract a file from Kurumi. (Valid: config, items, kurumidata, commands, en)", Extract),
            ("convertdb", "Copies the old json based database to MongoDB (The old db folder must be renamed to Database_Migrate)", ConvertDB)
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

            bool Found = false;
            for (int i = 0; i < Commands.Count; i++)
            {
                var (cmd, help, Handler) = Commands[i];
                if (cmd == command)
                {
                    Found = true;
                    Handler.Invoke(args);
                }
            }
            if (!Found)
                Console.WriteLine("Invalid command!");

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

            if (FileContent == null)
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
        #region DbMigrate
        private static void ConvertDB(string s)
        {
            Console.WriteLine("Preparing...");
            var sw = Stopwatch.StartNew();
            if (!ConfigurePath())
                return;
            Console.WriteLine("Loading config.");
            if (!Config.TryLoad(out Exception ex))
            {
                Console.WriteLine(ex);
                return;
            }
            Console.WriteLine("Loaded config.");

            Console.WriteLine("Connecting to database...");
            string ConnectionString = "mongodb://";
            if (Config.MongoDBUsername != null)
                ConnectionString += $"{Config.MongoDBUsername}:{Config.MongoDBPassword}@";
            ConnectionString += $"{Config.MongoDBIp}:{Config.MongoDBPort}";
            var DBClient = new MongoClient(ConnectionString);
            var Database = DBClient.GetDatabase(Config.MongoDBName);
            Console.WriteLine("Connected to database.");

            string DbPath = $"{KurumiPathConfig.Root}Database_Migrate";
            string UserDbPath = $"{DbPath}{KurumiPathConfig.Separator}UserDatabase";
            string GuildDbPath = $"{DbPath}{KurumiPathConfig.Separator}ServerDatabase";
            string CharacterDbPath = $"{DbPath}{KurumiPathConfig.Separator}CharacterDatabase";
            if (!Directory.Exists(DbPath) ||
                !Directory.Exists(UserDbPath) ||
                !Directory.Exists(GuildDbPath) ||
                !Directory.Exists(CharacterDbPath))
            {
                Console.WriteLine("Not found!");
                return;
            }

            Console.WriteLine("Starting to migrate to MongoDB. This may take a while.");
            #region UserDatabase
            Console.WriteLine("Loading UserDatabase...");
            var UserCollection = Database.GetCollection<User>("User");
            string[] Users = Directory.GetDirectories(UserDbPath);
            for (int i = 0; i < Users.Length; i++)
            {
                string u = Users[i];
                Console.Write($"\rProcessing: {i + 1}/{Users.Length} | {new DirectoryInfo(u).Name}");
                string FilePath = $"{u}{KurumiPathConfig.Separator}User.json";
                string Content = File.ReadAllText(FilePath);
                var User = JsonConvert.DeserializeObject<Old_User>(Content);
                UserCollection.InsertOne(new User() { Credit = User.Credit, Exp = User.Exp, UserId = ulong.Parse(new DirectoryInfo(u).Name) });
            }
            Console.WriteLine("\nDone!");
            #endregion UserDatabase

            #region GuildDatabase
            Console.WriteLine("Loading GuildDatabase...");
            var GuildCollection = Database.GetCollection<Guild>("Guild");
            string[] Guilds = Directory.GetDirectories(GuildDbPath);
            for (int i = 0; i < Guilds.Length; i++)
            {
                var g = Guilds[i];
                Console.Write($"\rProcessing: {i + 1}/{Guilds.Length} | {new DirectoryInfo(g).Name}");
                var NewGuild = new Guild()
                {
                    GuildId = ulong.Parse(new DirectoryInfo(g).Name)
                };
                //Load config
                string cfgPath = $"{g}{KurumiPathConfig.Separator}Server.json";
                if (File.Exists(cfgPath))
                {
                    string cfgContent = File.ReadAllText(cfgPath);
                    var cfg = JsonConvert.DeserializeObject<Old_GuildConfig>(cfgContent);
                    NewGuild.Prefix = cfg.Prefix;
                    NewGuild.Lang = cfg.Lang;
                    NewGuild.PunishmentForWord = cfg.PunishmentForWord;
                    NewGuild.PunishmentForWarning = cfg.PunishmentForWarning;
                    NewGuild.WelcomeChannel = cfg.WelcomeChannel;
                    NewGuild.Ranking = cfg.Ranking;
                    NewGuild.MaxWarnings = (uint)cfg.MaxWarnings;
                    NewGuild.Volume = cfg.Volume;
                    NewGuild.Increment = cfg.Inc;
                    NewGuild.BlacklistedWords = cfg.BlakclistedWords;
                    NewGuild.WelcomeMessages = cfg.WelcomeMessages;
                    NewGuild.ColorRoles = cfg.ColorRoles;
                }

                //Load permissions
                string permPath = $"{g}{KurumiPathConfig.Separator}Permissions";
                if (Directory.Exists(permPath))
                {
                    string[] permFiles = Directory.GetFiles(permPath);
                    for (int j = 0; j < permFiles.Length; j++)
                    {
                        string[] permissions = File.ReadAllLines(permFiles[j]);
                        ulong RoleId = ulong.Parse(new FileInfo(permFiles[j]).Name.Remove(".perm").Remove(".PERM"));
                        NewGuild.Permissions.Add(new RolePermissions()
                        {
                            RoleId = RoleId,
                            PermissionList = permissions.ToList()
                        });
                    }
                }

                //Load messages
                string msgPath = $"{g}{KurumiPathConfig.Separator}Messages";
                if (Directory.Exists(msgPath))
                {
                    string[] messages = Directory.GetFiles(msgPath);
                    for (int j = 0; j < messages.Length; j++)
                    {
                        ulong msgId = ulong.Parse(new FileInfo(messages[j]).Name.Remove(".json"));
                        string msgContent = File.ReadAllText(messages[j]);
                        Old_Message msg = null;
                        try
                        {
                            msg = JsonConvert.DeserializeObject<Old_Message>(msgContent);
                        }
                        catch (Exception) { }
                        if (msg == null)
                            continue;
                        NewGuild.Messages.Add(new DeletedMessage() { MessageId = msgId, SentAt = msg.SentAt, SentBy = msg.SentBy, Text = msg.Text });
                    }
                }

                //Load rewards
                string rewardsPath = $"{g}{KurumiPathConfig.Separator}Rewards.json";
                if (File.Exists(rewardsPath))
                {
                    string rewardsContent = File.ReadAllText(rewardsPath);
                    var rewards = JsonConvert.DeserializeObject<Old_Reward>(rewardsContent);
                    foreach (var reward in rewards.Rewards)
                    {
                        NewGuild.Rewards.Add(new Reward() { Level = (uint)reward.Key, Role = reward.Value });
                    }
                }

                //Load users & warnings
                string warningsPath = $"{g}{KurumiPathConfig.Separator}Warnings.json";
                List<Old_Warning> Warnings = null;
                if (File.Exists(warningsPath))
                {
                    string warningsContent = File.ReadAllText(warningsPath);
                    Warnings = JsonConvert.DeserializeObject<List<Old_Warning>>(warningsContent);
                }

                string usersPath = $"{g}{KurumiPathConfig.Separator}Users";
                if (Directory.Exists(usersPath))
                {
                    string[] users = Directory.GetDirectories(usersPath);
                    for (int j = 0; j < users.Length; j++)
                    {
                        string filePath = $"{users[j]}{KurumiPathConfig.Separator}User.json";
                        ulong Id = ulong.Parse(new DirectoryInfo(users[j]).Name);
                        string fileContent = File.ReadAllText(filePath);
                        var user = JsonConvert.DeserializeObject<Old_User>(fileContent);

                        uint warnings = 0;
                        if (Warnings != null)
                        {
                            for (int k = 0; k < Warnings.Count; k++)
                            {
                                if (Warnings[k].UserId == Id)
                                    warnings = (uint)Warnings[k].Count;
                            }
                        }
                        NewGuild.Users.Add(new GUser() { Exp = user.Exp, Warnings = warnings, AfkMessage = null, UserId = Id });
                    }
                }

                //Insert into db
                GuildCollection.InsertOne(NewGuild);
            }
            Console.WriteLine("\nDone!");
            #endregion GuildDatabase

            #region CharacterDatabase
            Console.WriteLine("Loading CharacterDatabase...");
            var CharacterCollection = Database.GetCollection<Character>("Character");
            string[] Characters = Directory.GetDirectories(CharacterDbPath);
            for (int i = 0; i < Characters.Length; i++)
            {
                var c = Characters[i];
                var Name = new DirectoryInfo(c).Name;
                Console.Write($"\rProcessing: {i + 1}/{Characters.Length} | {Name}             ");
                string characterContent = File.ReadAllText($"{c}{KurumiPathConfig.Separator}Character.json");
                var Character = JsonConvert.DeserializeObject<Old_Character>(characterContent);


                if (Character.Data.ProfilePicture != null)
                {
                    var pfpPath = $"{c}{KurumiPathConfig.Separator}{Character.Data.ProfilePicture}";
                    if (File.Exists(pfpPath))
                    {
                        NewName:
                        var targetPath = $"{KurumiPathConfig.ProfilePictures}{Character.Data.ProfilePicture}";
                        if (File.Exists(targetPath))
                        {
                            Character.Data.ProfilePicture = "1" + Character.Data.ProfilePicture;
                            goto NewName;
                        }
                        File.Copy(pfpPath, targetPath);
                    }
                    else
                        Character.Data.ProfilePicture = null;
                }

                List<PlayerItem> Inv = new List<PlayerItem>();
                for (int l = 0; l < Character.Equipment.Inventory.Count; l++)
                    Inv.Add(ToPlayerItem(Character.Equipment.Inventory[l]));
                CharacterCollection.InsertOne(new Character()
                {
                    Name = Name,
                    ProfilePicture = Character.Data.ProfilePicture,
                    Exp = Character.Data.Exp,
                    Ai = Character.Data.Ai,
                    Owner = Character.Data.Owner,
                    Weapon = ToPlayerItem(Character.Equipment.Weapon),
                    Boots = ToPlayerItem(Character.Equipment.Boots),
                    Hat = ToPlayerItem(Character.Equipment.Hat),
                    Shirt = ToPlayerItem(Character.Equipment.Shirt),
                    Coat = ToPlayerItem(Character.Equipment.Coat),
                    Glove = ToPlayerItem(Character.Equipment.Glove),
                    Leggings = ToPlayerItem(Character.Equipment.Leggings),
                    A = Character.Equipment.A,
                    X = Character.Equipment.X,
                    Y = Character.Equipment.Y,
                    Inventory = Inv
                });
            }
            Console.WriteLine("\nDone!");
            #endregion CharacterDatabase

            sw.Stop();
            Console.WriteLine($"Finished migrating! Took: {sw.ElapsedMilliseconds} ms");
        }

        private class Old_User
        {
            public uint Exp { get; set; }
            public uint Credit { get; set; }
        }
        private class Old_GuildConfig
        {
            public string Prefix { get; set; } = "!k.";
            public string Lang { get; set; } = "en";
            public string PunishmentForWord { get; set; } = "Warning";
            public string PunishmentForWarning { get; set; } = "Ban";
            public ulong WelcomeChannel { get; set; } = 0;
            public bool Ranking { get; set; } = true;
            public int MaxWarnings { get; set; } = 4;
            public float Volume { get; set; } = 100;
            public int Inc { get; set; } = -1;
            public List<string> BlakclistedWords { get; set; } = new List<string>();
            public List<string> WelcomeMessages { get; set; } = new List<string>();
            public List<ulong> ColorRoles { get; set; } = new List<ulong>();
        }
        private class Old_Message
        {
            public string Text { get; set; }
            public DateTime SentAt { get; set; }
            public string SentBy { get; set; }
        }
        private class Old_Reward
        {
            public Dictionary<int, ulong> Rewards { get; set; } = new Dictionary<int, ulong>();
        }
        private class Old_Warning
        {
            public ulong UserId { get; set; }
            public int Count { get; set; }
        }
        private class Old_Character
        {
            public Old_CharacterData Data { get; set; }
            public Old_CharacterEquipment Equipment { get; set; }
        }
        private class Old_CharacterData
        {
            public string ProfilePicture { get; set; }
            public int Exp { get; set; }
            public bool Ai { get; set; }
            public ulong Owner { get; set; }
        }
        private class Old_CharacterEquipment
        {
            public Old_PlayerItem Weapon { get; set; }
            public Old_PlayerItem Boots { get; set; }
            public Old_PlayerItem Hat { get; set; }
            public Old_PlayerItem Shirt { get; set; }
            public Old_PlayerItem Coat { get; set; }
            public Old_PlayerItem Glove { get; set; }
            public Old_PlayerItem Leggings { get; set; }
            public List<Old_PlayerItem> Inventory { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int A { get; set; }
        }
        private class Old_PlayerItem
        {
            public int Id { get; set; }
            public long Durability { get; set; }
            public Dictionary<string, int> CustomEffects { get; set; }

            public Old_PlayerItem(int Id, int Durability, Dictionary<string, int> CustomEffects)
            {
                this.Id = Id;
                this.Durability = Durability;
                this.CustomEffects = CustomEffects;
            }
        }

        private static PlayerItem ToPlayerItem(Old_PlayerItem item)
        {
            return new PlayerItem(item.Id, item.Durability, item.CustomEffects);
        }
        #endregion DbMigrate
        #endregion Commands

        private static bool ConfigurePath()
        {
            if (PathConfigured)
                return true;
            KurumiPathConfig.TryConfigure(out Exception ex);
            if (ex != null)
            {
                Console.WriteLine("Failed to configure paths! Exception:\n" + ex);
                return false;
            }
            return PathConfigured = true;
        }
    }
}