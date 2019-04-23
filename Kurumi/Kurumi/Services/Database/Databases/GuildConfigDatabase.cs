using Kurumi.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Services.Database.Databases
{
    public class GuildConfigDatabase : KurumiDatabase<GuildConfig>, IKurumiDatabase
    {
        public const uint INC_GLOBAL = 50;
        public const uint INC_GUILD = 30;

        public void Load()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            //Set cache
            Cache = new ConcurrentDictionary<ulong, GuildConfig>();
            //Get all guilds from the folder
            string[] Guilds = Directory.GetDirectories(KurumiPathConfig.GuildDatabase);
            //Parse all
            for(int i = 0; i < Guilds.Length; i++)
            {
                string Current = Guilds[i];
                //Parse id
                string sId = new DirectoryInfo(Current).Name;
                ulong Id = ulong.Parse(sId);
                //Parse data
                GuildConfig guild = new GuildConfig();
                string p = $"{Current}{KurumiPathConfig.Separator}Server.json";
                if (File.Exists(p)) //If this file doesn't exists the server doesn't have any custom settings
                {
                    string Content = File.ReadAllText(p);
                    guild = JsonConvert.DeserializeObject<GuildConfig>(Content);
                    if (guild == null)
                        guild = new GuildConfig();

                    if (guild.Inc == -1)
                        guild.Inc = (int)INC_GUILD;
                }
                //Cache data
                Cache.TryAdd(Id, guild);
                //Update console
                Console.Write($"\r#Loaded: {i + 1}/{Guilds.Length} | Current: {sId}           ");
            }
            Console.Write("\r                                                                                               "); //Clear the line
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\r#Guilds loaded.\n");
        }

        public void Save(bool Show)
        {
            //Copy cache
            var CopyCache = new ConcurrentDictionary<ulong, GuildConfig>(Cache);
            //Save all entries in cache
            uint i = 1;
            foreach(var Entry in CopyCache)
            {
                if (Entry.Value.Inc == INC_GUILD)
                    Entry.Value.Inc = -1;

                string Content = JsonConvert.SerializeObject(Entry.Value);
                string Path = $"{KurumiPathConfig.GuildDatabase}{Entry.Key}";
                Directory.CreateDirectory(Path);
                Path += $"{KurumiPathConfig.Separator}Server.json";
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
                Console.Write("\r#Guilds saved.\n");
            }
        }
    }
}