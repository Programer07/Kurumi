using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Models;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Database.Databases
{
    public class GuildDatabase : KurumiDatabase<Guild>, IKurumiDatabase
    {
        public const uint INC_GLOBAL = 50;
        public const uint INC_GUILD = 30;

        public static GUser GetOrCreate(ulong GuildId, ulong UserId)
        {
            var g = GetOrCreate(GuildId);
            if (g._Users.ContainsKey(UserId))
                return g._Users[UserId];
            var u = new GUser();
            g._Users.TryAdd(UserId, u);
            return u;
        }
        public static GUser GetOrFake(ulong GuildId, ulong UserId)
        {
            var g = GetOrFake(GuildId);
            if (g != null && g._Users.ContainsKey(UserId))
                return g._Users[UserId];
            return new GUser();
        }
        public static GUser Get(ulong GuildId, ulong UserId)
        {
            var g = Get(GuildId);
            if (g == null || !g._Users.ContainsKey(UserId))
                return null;
            return g._Users[UserId];
        }
        public static void Set(ulong GuildId, ulong UserId, GUser Value)
        {
            var g = Get(GuildId);
            if (Value == null && g == null)
                return;
            else if (g == null)
                g = new Guild();

            if (Value == null)
                g._Users.TryRemove(UserId, out _);
            else
                g._Users.TryAdd(UserId, Value);
        }


        public void Load()
        {
            Cache = new ConcurrentDictionary<ulong, Guild>();
            ConsoleHelper.Write("Waiting for MongoDB...", ConsoleColor.Yellow);
            var Guilds = DatabaseManager.Database.GetCollection<Guild>("Guild").Find(_ => true).ToList();
            for (int i = 0; i < Guilds.Count; i++)
            {
                var guild = Guilds[i];
                ConsoleHelper.Write($"Loading: {i}/{Guilds.Count} | Current: {guild.GuildId}", ConsoleColor.Yellow);

                if (guild.Increment == -1)
                    guild.Increment = (int)INC_GUILD;

                for (int j = 0; j < guild.Users.Count; j++)
                {
                    var user = guild.Users[j];
                    guild._Users.TryAdd(user.UserId, user);
                }
                guild.Users = new List<GUser>(); //Reseting the list to use less memory, this list is only used on loading and saving
                Cache.TryAdd(guild.GuildId, guild);
            }
            ConsoleHelper.ClearCurrentLine();
            ConsoleHelper.WriteLine("Guilds loaded.", ConsoleColor.Green);
        }

        public void Save(bool Show)
        {
            if (Cache.Count == 0)
                return;
            var Collection = DatabaseManager.Database.GetCollection<Guild>("Guild_Temp");
            var CacheCopy = new ConcurrentDictionary<ulong, Guild>(Cache);
            int i = 0;
            foreach (var Guild in CacheCopy)
            {
                if (Show)
                {
                    i++;
                    ConsoleHelper.Write($"Saving: {i}/{CacheCopy.Count} | Current: {Guild.Key}", ConsoleColor.Yellow);
                }

                foreach (var User in Guild.Value._Users)
                {
                    User.Value.UserId = User.Key;
                    Guild.Value.Users.Add(User.Value);
                }
                if (Guild.Value.Increment == (int)INC_GUILD)
                    Guild.Value.Increment = -1;
                Guild.Value.GuildId = Guild.Key;
                Collection.InsertOne(Guild.Value);
                Guild.Value.Users = new List<GUser>();
            }
            DatabaseManager.Database.DropCollection("Guild");
            DatabaseManager.Database.RenameCollection("Guild_Temp", "Guild");
            if (Show)
            {
                ConsoleHelper.ClearCurrentLine();
                ConsoleHelper.Write("Guilds saved.\n", ConsoleColor.Green);
            }
        }
    }
}