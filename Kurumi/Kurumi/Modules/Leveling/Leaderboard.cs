using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using Kurumi.Services.Leveling;
using System;
using System.Collections.Concurrent;
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
                embed.WithTitle(lang["leaderboard_title", "BOARD", leaderboard == 0 ? "Global" : "Server", "PAGE", (Page + 1).ToString(), "MAX", Math.Ceiling((double)Ranking.Count / PAGE_LENGTH).ToString()]);
                //Add all user
                uint Increment = leaderboard == 0 ? GuildDatabase.INC_GLOBAL : (uint)GuildDatabase.GetOrFake(Context.Guild.Id).Increment;
                for (int i = 0; i < PAGE_LENGTH; i++)
                {
                    int Index = Page * PAGE_LENGTH + i;
                    if (Index >= Ranking.Count)
                        break;
                    IUser User = Context.Client.GetUserAsync(Ranking[Index]).Result;
                    uint Exp;
                    if (leaderboard == 0)
                        Exp = UserDatabase.Get(User.Id).Exp;
                    else
                        Exp = GuildDatabase.Get(Context.Guild.Id, User.Id).Exp;
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

        [Command("resetleaderboard")]
        [Alias("clearleaderboard")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearLeaderboard([Optional, Remainder]string User)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (User != null)
                {
                    IUser user = await Utilities.GetUser(Context.Guild, User);
                    if (user == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["resetleaderboard_not_found"]);
                        return;
                    }
                    var guild = GuildDatabase.GetOrFake(Context.Guild.Id);
                    if (guild._Users.ContainsKey(user.Id))
                        guild._Users.TryRemove(user.Id, out _);
                    await Context.Channel.SendEmbedAsync(lang["resetleaderboard_single"]);
                }
                else
                {
                    await Context.Channel.SendEmbedAsync(lang["resetleaderboard_start"]);
                    GuildDatabase.GetOrFake(Context.Guild.Id)._Users = new ConcurrentDictionary<ulong, GUser>();
                    await Context.Channel.SendEmbedAsync(lang["resetleaderboard_done"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ResetLeaderboard", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ResetLeaderboard", null, ex), Context);
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
                var GuildUsers = GuildDatabase.GetOrFake(Context.Guild.Id)._Users;
                foreach (var User in GuildUsers)
                {
                    if ((await Context.Guild.GetUserAsync(User.Key)) == null)
                    {
                        GuildDatabase.Set(Context.Guild.Id, Context.User.Id, null);
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