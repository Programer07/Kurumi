using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Leveling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Leveling
{
    public class Leaderboard : ModuleBase
    {
        private const byte PAGE_LENGTH = 5;
        [Command("leaderboard")]
        [RequireContext(ContextType.Guild)]
        public async Task ShowLeaderboard([Optional, Remainder]string page)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);

                byte leaderboard = 1; //1 - server, 0 - global
                int Page = 0;
                //Leaderboard & page select
                if (page != null)
                {
                    if (page.StartsWith("global"))
                    {
                        leaderboard = 0;
                        if (page.Length >= 7)
                            page = page.Substring(7);
                    }
                    else if (page.StartsWith("server"))
                    {
                        leaderboard = 1;
                        if (page.Length >= 7)
                            page = page.Substring(7);
                    }
                    else if (page.StartsWith("guild"))
                    {
                        leaderboard = 1;
                        if (page.Length >= 6)
                            page = page.Substring(6);
                    }
                    else if (!int.TryParse(page, out Page))
                    {
                        await Context.Channel.SendEmbedAsync(lang["leaderboard_invalid_page"]);
                        return;
                    }
                    if (!int.TryParse(page, out Page)) Page = 1;
                    Page--;
                }
                //Get user list
                var Ranking = ExpManager.GetRanking(Context.Guild, Context.Client)[leaderboard];
                //Set the page to the last page if the selected page is > then the last
                if (Ranking.Count / PAGE_LENGTH < Page)
                    Page = Ranking.Count / PAGE_LENGTH;
                //Create embed
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithColor(Config.EmbedColor);
                embed.WithTitle(lang["leaderboard_title", "BOARD", leaderboard == 0 ? "Global" : "Server", "PAGE", (Page + 1).ToString(), "MAX", Math.Ceiling((double)Ranking.Count / 10).ToString()]);
                //Add all user
                uint Increment = leaderboard == 0 ? GuildConfigDatabase.INC_GLOBAL : (uint)GuildConfigDatabase.GetOrFake(Context.Guild.Id).Inc;
                for (int i = 0; i < PAGE_LENGTH; i++)
                {
                    int Index = Page * PAGE_LENGTH + i;
                    if (Index >= Ranking.Count)
                        break;
                    IUser User = Context.Client.GetUserAsync(Ranking[Index]).Result;
                    uint Exp;
                    if (leaderboard == 0)
                        Exp = GlobalUserDatabase.Get(User.Id).Exp;
                    else
                        Exp = GuildUserDatabase.Get(Context.Guild.Id, User.Id).Exp;
                    embed.AddField($"#{Index + 1}) {User.Username}", lang["leaderboard_user", "EXP", Exp, "LEVEL", ExpManager.Level(Exp, Increment)]);
                }
                //Send
                await Context.Channel.SendMessageAsync("", embed: embed.Build());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Leaderboard", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Leaderboard", null, ex), Context);
            }
        }

        [Command("clearleaderboard")]
        [Alias("resetleaderboard")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearLeaderboard()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync(lang["clearleaderboard_start"]);
                ExpManager.ClearGuildRanking(Context.Guild);
                await Context.Channel.SendEmbedAsync(lang["clearleaderboard_done"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ClearLeaderboard", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ClearLeaderboard", null, ex), Context);
            }
        }

        [Command("pruneleaderboard")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task PruneLeaderboard()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync(lang["pruneleaderboard_start"]);
                var GuildUsers = GuildUserDatabase.GetOrFake(Context.Guild.Id);
                foreach (var User in GuildUsers)
                {
                    if ((await Context.Guild.GetUserAsync(User.Key)) == null)
                    {
                        string path = $"{KurumiPathConfig.GuildDatabase}{Context.Guild.Id}{KurumiPathConfig.Separator}Users";
                        if (Directory.Exists(path))
                            Directory.Delete(path, true);
                        GuildUsers.TryRemove(User.Key, out _);
                    }
                }
                await Context.Channel.SendEmbedAsync(lang["pruneleaderboard_done"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "PruneLeaderboard", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "PruneLeaderboard", null, ex), Context);
            }
        }
    }
}