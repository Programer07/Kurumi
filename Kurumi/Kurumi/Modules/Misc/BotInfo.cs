using Discord;
using Discord.Commands;
using Kurumi.Modules.Music;
using Kurumi.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Kurumi.Modules.Misc
{
    public class BotInfo : ModuleBase
    {
        [Command("botinfo")]
        [Alias("bi")]
        public async Task SendBotInfo()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                //Get current process
                Process proc = Process.GetCurrentProcess();
                //Get discord app
                IApplication App = await Context.Client.GetApplicationInfoAsync();
                //Calculate time
                TimeSpan Time = DateTime.Now - proc.StartTime;
                string FancyTime = $"{(Time.Days > 0 ? $"{Time.Days}d " : "")}{(Time.Days > 0 ? $"{Time.Hours}h " : "")}{(Time.Minutes > 0 ? $"{Time.Minutes}m " : "")}{Time.Seconds}s";
                //Get guilds
                IReadOnlyCollection<IGuild> Guilds = Context.Client.GetGuildsAsync().Result;
                //Get Kurumi data
                KurumiData d = KurumiData.Get();
                //Setup embed
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .WithImageUrl(App.IconUrl)
                    .AddField(lang["botinfo_info_title"], lang["botinfo_info_field",
                                                                                        "UTIME", FancyTime,
                                                                                        "SCOUNT", Guilds.Count.ToString(),
                                                                                        "UCOUNT", Guilds.Sum(x => x.GetUsersAsync().Result.Count).ToString(),
                                                                                        "MCOUNT", Music.Music.MusicPlayers.Count], true)
                    .AddField(lang["botinfo_version_title"], lang["botinfo_version_field", "KVERSION", d.Version, "AVERSION", d.ApiVersion], true)
                    .AddField(lang["botinfo_online_title"], lang["botinfo_online_field"], true);
                //Send
                await Context.Channel.SendMessageAsync("", embed: embed.Build());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "BotInfo", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "BotInfo", null, ex), Context);
            }
        }
    }
}