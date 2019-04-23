using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Models;

namespace Kurumi.Services.Leveling
{
    public class ExpManager
    {
        private static (DateTime time, List<ulong>[] ranks) RankingCache = (new DateTime(1970, 1, 1), null);

        public static async Task AddExp(ICommandContext context, uint Amount = 1)
        {
            try
            {
                var lang = Language.GetLanguage(context.Guild);

                //Global level
                var User = GlobalUserDatabase.GetOrCreate(context.User.Id);
                byte level = Level(User.Exp, GuildConfigDatabase.INC_GLOBAL);
                User.Exp += Amount;
                byte newLevel = Level(User.Exp, GuildConfigDatabase.INC_GLOBAL);

                //Send message if new level is greater then old level
                if (level < newLevel)
                    await context.Channel.TrySendEmbedAsync(lang["leveling_level_up_global", "USER", context.User.Username, "LEVEL", newLevel]);


                //Server level

                //Get increment
                var config = GuildConfigDatabase.Get(context.Guild.Id);
                var increment = config == null ? GuildConfigDatabase.INC_GUILD : (uint)config.Inc;

                //Add exp
                var gUser = GuildUserDatabase.GetOrCreate(context.Guild.Id, context.User.Id);
                level = Level(gUser.Exp, increment);
                gUser.Exp += Amount;
                newLevel = Level(gUser.Exp, increment);

                //Send message and give reward if set
                if (level < newLevel)
                {
                    await context.Channel.TrySendEmbedAsync(lang["leveling_level_up_server", "USER", context.User.Username, "LEVEL", newLevel]);

                    //Get rewards and check if there is a set reward for the new level
                    var Rewards = RewardDatabase.Get(context.Guild.Id)?.Rewards;
                    if(Rewards != null && Rewards.ContainsKey(newLevel))
                    {
                        IRole rewardRole = context.Guild.GetRole(Rewards[newLevel]);
                        if (rewardRole == null)
                            return;

                        try
                        {
                            await (context.User as IGuildUser).AddRoleAsync(rewardRole);
                            await context.Channel.TrySendEmbedAsync(lang["leveling_reward_received", "USER", context.User.Username, "ROLE", rewardRole.Name, "LEVEL", newLevel]);
                        }
                        catch(Exception) //No permission, ignore
                        { }
                    }
                }
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ExpManager", null, ex), context);
            }
        }

        public static List<ulong>[] GetRanking(IGuild guild, IDiscordClient client)
        {
            //Check cache
            if (DateTime.Now.Subtract(RankingCache.time).TotalSeconds < 30)
                return RankingCache.ranks;
            //Calculate global rank
            Dictionary<ulong, uint> RankableUsers = new Dictionary<ulong, uint>();
            foreach (var user in GlobalUserDatabase.Cache)
            {
                IUser u = client.GetUserAsync(user.Key).Result;
                if (u != null && user.Value != null)
                    RankableUsers.Add(user.Key, user.Value.Exp);
            }
            Dictionary<ulong, uint> GlobalRankedUsers = (from entry in RankableUsers orderby entry.Value descending select entry).ToDictionary(x => x.Key, x => x.Value);
            //Calculate server rank
            var ServerUsers = GuildUserDatabase.Get(guild.Id);
            Dictionary<ulong, uint> ServerRankableUsers = new Dictionary<ulong, uint>();
            foreach (var user in ServerUsers)
            {
                IUser u = guild.GetUserAsync(user.Key).Result;
                if (u != null && user.Value != null)
                    ServerRankableUsers.Add(user.Key, user.Value.Exp);
            }
            Dictionary<ulong, uint> ServerRankedUsers = (from entry in ServerRankableUsers orderby entry.Value descending select entry).ToDictionary(x => x.Key, x => x.Value);
            //Add global ranks
            List<ulong> Users = new List<ulong>();
            foreach (var User in GlobalRankedUsers)
                Users.Add(User.Key);
            //Add server ranks
            List<ulong> SUsers = new List<ulong>();
            foreach (var User in ServerRankedUsers)
                SUsers.Add(User.Key);
            //Cache and return list
            var Ranking = new List<ulong>[2] { Users, SUsers };
            RankingCache = (DateTime.Now, Ranking);
            return Ranking;
        }

        public static void ClearGuildRanking(IGuild guild)
        {
            var path = $"{KurumiPathConfig.GuildDatabase}{guild.Id}{KurumiPathConfig.Separator}Users";
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            GuildUserDatabase.Set(guild.Id, null);
        }

        public static byte Level(uint Exp, uint Increment)
            => (byte)((int)(1 + Math.Sqrt(1 + 8 * Exp / Increment)) / 2);
        public static uint LevelStartExp(uint Level, uint Increment)
            => (Level * Level - Level) * Increment / 2;
    }

    public enum LevelUpType
    {
        Global,
        Guild
    }
}