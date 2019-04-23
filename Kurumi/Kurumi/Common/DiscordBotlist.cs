using Discord;
using Kurumi.Services.Database;
using Kurumi.StartUp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Common
{
    public class DiscordBotlist
    {
        public static async Task UpdateServerCount()
        {
            try
            {
                if (Config.Environment == Config.KurumiEnvironment.Development)
                    return;
                ulong KurumiId = Program.Bot.DiscordClient.CurrentUser.Id;
                using (var http = new HttpClient())
                {
                    using (var content = new FormUrlEncodedContent(
                        new Dictionary<string, string> {
                                        { "server_count", Program.Bot.DiscordClient.Guilds.Count.ToString() }
                        }))
                    {
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        http.DefaultRequestHeaders.Add("Authorization", Config.BotlistApiKey);
                        await http.PostAsync($"https://discordbots.org/api/bots/{KurumiId}/stats", content).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "DiscordBotList", null, ex));
            }
        }

        public static async Task<bool> UserVoted(ulong user)
        {
            try
            {
                if (Config.BotlistApiKey == null)
                    return true; //Disabled, self hosted bots shouldn't have limitations.
                await Get();
                if (!File.Exists(KurumiPathConfig.DbRoot + "Voters.json"))
                    return false;
                string VoterList = File.ReadAllText(KurumiPathConfig.DbRoot + "Voters.json");
                return VoterList.Contains(user.ToString());
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task Get()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Add("Authorization", Config.BotlistApiKey);
                    string res = await http.GetStringAsync($"https://discordbots.org/api/bots/374274129282596885/votes");
                    User[] users = JsonConvert.DeserializeObject<User[]>(res);

                    string voterList = null;
                    foreach (User voter in users)
                        voterList += voter.id + ",";
                    File.WriteAllText(KurumiPathConfig.DbRoot + "Voters.json", voterList);
                }

            }
            catch (Exception)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Warning, "Vote refresh", "Failed to refresh!"));
            }
        }

        private class User
        {
            public string username { get; set; }
            public string discriminator { get; set; }
            public string id { get; set; }
            public string avatar { get; set; }
        }
    }
}