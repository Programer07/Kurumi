using Kurumi.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Services.Database.Databases
{
    public class AfkMessageDatabase : KurumiDatabase<List<AfkMessage>>, IKurumiDatabase
    {
        public void Load()
        {
            Cache = new ConcurrentDictionary<ulong, List<AfkMessage>>();
            int i = 1;
            foreach (var Entry in GuildConfigDatabase.Cache.Keys)
            {
                string Path = $"{KurumiPathConfig.GuildDatabase}{Entry}{KurumiPathConfig.Separator}AfkMessages.json";
                if (File.Exists(Path))
                {
                    string Content = File.ReadAllText(Path);
                    var Messages = JsonConvert.DeserializeObject<List<AfkMessage>>(Content);
                    Cache.TryAdd(Entry, Messages);
                }
                Console.Write($"\r#Loaded: {i}/{GuildConfigDatabase.Cache.Count} | Current: {Entry}     ");
                i++;
            }
            Console.Write("\r                                                                                                  ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\r#Afk messages loaded.\n");
        }

        public void Save(bool Show)
        {
            //Copy cache
            var CacheCopy = new ConcurrentDictionary<ulong, List<AfkMessage>>(Cache);
            //Save
            int i = 1;
            foreach (var Entry in CacheCopy)
            {
                string Path = $"{KurumiPathConfig.GuildDatabase}{Entry.Key}";

                Directory.CreateDirectory(Path);
                Path += $"{KurumiPathConfig.Separator}AfkMessages.json";

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
                Console.Write("\r                                                                                  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\r#Afk messages saved.\n");
            }
        }
    }
}