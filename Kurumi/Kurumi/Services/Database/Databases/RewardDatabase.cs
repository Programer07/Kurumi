using Kurumi.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Services.Database.Databases
{
    public class RewardDatabase : KurumiDatabase<Reward>, IKurumiDatabase
    {
        public void Load()
        {
            int i = 1;
            Cache = new ConcurrentDictionary<ulong, Reward>();
            foreach (var Entry in GuildConfigDatabase.Cache.Keys)
            {
                string Path = $"{KurumiPathConfig.GuildDatabase}{Entry}{KurumiPathConfig.Separator}Rewards.json";
                if (File.Exists(Path))
                {
                    string Content = File.ReadAllText(Path);
                    var Warnings = JsonConvert.DeserializeObject<Reward>(Content);
                    Cache.TryAdd(Entry, Warnings);
                }
                Console.Write($"\r#Loaded: {i}/{GuildConfigDatabase.Cache.Count} | Current: {Entry}     ");
                i++;
            }
            Console.Write("\r                                                                                                           ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\r#Rewards loaded.\n");
        }

        public void Save(bool Show)
        {
            int i = 1;
            var CacheCopy = new ConcurrentDictionary<ulong, Reward>(Cache);
            foreach (var Entry in CacheCopy)
            {
                string Path = $"{KurumiPathConfig.GuildDatabase}{Entry.Key}";
                Directory.CreateDirectory(Path);
                Path += $"{KurumiPathConfig.Separator}Rewards.json";
                string Content = JsonConvert.SerializeObject(Entry.Value);
                File.WriteAllText(Path, Content);
                if (Show)
                {
                    Console.Write($"\r#Saved: {i}/{CacheCopy.Count} | Current: {Entry.Key}");
                    i++;
                }
            }
            if (Show)
            {
                Console.Write("\r                                                                                                           ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\r#Rewards saved.\n");
            }
        }
    }
}