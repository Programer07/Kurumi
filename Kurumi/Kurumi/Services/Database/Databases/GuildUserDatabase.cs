using Kurumi.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Kurumi.Services.Database.Databases
{
    public class GuildUserDatabase : KurumiDatabase<ConcurrentDictionary<ulong, GUser>>, IKurumiDatabase
    {
        public static GUser Get(ulong Key, ulong Key2)
        {
            var g = Get(Key);
            if (g != null && g.ContainsKey(Key2))
                return g[Key2];
            return null;
        }
        public static void Set(ulong Key, ulong Key2, GUser Value)
        {
            var g = Get(Key);
            if (g == null)
                g = new ConcurrentDictionary<ulong, GUser>();
            g.TryAdd(Key2, Value);
            Set(Key, g);
        }
        public static GUser GetOrCreate(ulong Key, ulong Key2)
        {
            var g = GetOrCreate(Key);

            if (g.ContainsKey(Key2))
                return g[Key2];

            var New = new GUser();
            g.TryAdd(Key2, New);
            return New;
        }

        public void Load()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            //Create cache
            Cache = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, GUser>>();
            //Get all guilds
            foreach (var guild in GuildConfigDatabase.Cache.Keys)
            {
                //Check if the server has saved users
                string Path = $"{KurumiPathConfig.GuildDatabase}{guild}{KurumiPathConfig.Separator}Users";
                if (Directory.Exists(Path))
                {
                    //Load every user
                    ConcurrentDictionary<ulong, GUser> ParsedUsers = new ConcurrentDictionary<ulong, GUser>();
                    string[] Users = Directory.GetDirectories(Path);
                    for(int i = 0; i < Users.Length; i++)
                    {
                        string Current = Users[i];
                        //Parse id
                        string sId = new DirectoryInfo(Current).Name;
                        ulong Id = ulong.Parse(sId);
                        //Parse content
                        GUser user = new GUser();
                        string p = $"{Path}{KurumiPathConfig.Separator}{Id}{KurumiPathConfig.Separator}User.json";
                        if(File.Exists(p))
                        {
                            string Content = File.ReadAllText(p);
                            user = JsonConvert.DeserializeObject<GUser>(Content);
                            if (user == null)
                                user = new GUser();
                        }
                        //Save data
                        ParsedUsers.TryAdd(Id, user);
                        //Update console
                        Console.Write($"\r#Saved: {i + 1}/{Users.Length} | Current: {sId}           ");
                    }
                    //Cache data
                    Cache.TryAdd(guild, ParsedUsers);
                }
            }
            Console.Write("\r                                                                                               "); //Clear the line
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\r#Server users loaded.\n");
        }

        public void Save(bool Show)
        {
            var CacheCopy = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, GUser>>(Cache);
            foreach (var Entry in CacheCopy)
            {
                int i = 1;
                string Path = $"{KurumiPathConfig.GuildDatabase}{Entry.Key}{KurumiPathConfig.Separator}";
                foreach (var User in Entry.Value)
                {
                    string fullPath = $"{Path}Users{KurumiPathConfig.Separator}{User.Key}";
                    Directory.CreateDirectory(fullPath);
                    fullPath += $"{KurumiPathConfig.Separator}User.json";
                    string Content = JsonConvert.SerializeObject(User.Value);
                    File.WriteAllText(fullPath, Content);
                    if (Show)
                    {
                        //Update console
                        Console.Write($"\r#Saved: {i}/{Entry.Value.Count} | Current: {User.Key}           ");
                        i++;
                    }
                }
            }
            if (Show)
            {
                Console.Write("\r                                                                                               "); //Clear the line
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\r#Server users loaded.\n");
            }
        }
    }
}