using Kurumi.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Services.Database.Databases
{
    public class GlobalUserDatabase : KurumiDatabase<User>, IKurumiDatabase
    {
        public void Load()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            //Set cache
            Cache = new ConcurrentDictionary<ulong, User>();
            //Get all servers from the folder
            string[] Users = Directory.GetDirectories(KurumiPathConfig.UserDatabase);
            //Parse all
            for (int i = 0; i < Users.Length; i++)
            {
                string Current = Users[i];
                //Parse id
                string sId = new DirectoryInfo(Current).Name;
                ulong Id = ulong.Parse(sId);
                //Parse data
                User user = new User();
                string p = $"{Current}{KurumiPathConfig.Separator}User.json";
                if (File.Exists(p)) //In the current update it will always be true
                {
                    string Content = File.ReadAllText(p);
                    user = JsonConvert.DeserializeObject<User>(Content);
                    if (user == null)
                        user = new User();
                }
                //Cache data
                Cache.TryAdd(Id, user);
                //Update console
                Console.Write($"\r#Loaded: {i + 1}/{Users.Length} | Current: {sId}           ");
            }
            Console.Write("\r                                                                                               "); //Clear the line
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\r#Users loaded.\n");
        }

        public void Save(bool Show)
        {
            //Copy cache
            var CacheCopy = new ConcurrentDictionary<ulong, User>(Cache);
            //Save all entries in cache
            uint i = 1;
            foreach (var Entry in CacheCopy)
            {
                string Content = JsonConvert.SerializeObject(Entry.Value);
                string Path = $"{KurumiPathConfig.UserDatabase}{Entry.Key}";
                Directory.CreateDirectory(Path);
                Path += $"{KurumiPathConfig.Separator}User.json";
                File.WriteAllText(Path, Content);

                if (Show)
                {
                    Console.Write($"\r#Saved: {i}/{Cache.Count} | Current: {Entry.Key}           ");
                    i++;
                }
            }
            //Clear the line
            if (Show)
            {
                Console.Write("\r                                                                                               ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\r#Users saved.\n");
            }
        }
    }
}