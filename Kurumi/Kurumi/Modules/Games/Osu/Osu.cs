using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games.Osu
{
    public class Osu : ModuleBase
    {
        [Command("osu")]
        public async Task UserInfo([Optional]string mode, [Optional, Remainder]string player)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Config.OsuApiKey == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["command_disabled"]);
                    return;
                }

                //Get player name and mode
                var Mode = OsuGameMode.Standard;
                if (player == null)
                {
                    if (mode != null)
                        player = mode;
                    else
                    {
                        await Context.Channel.SendEmbedAsync(lang["osu_no_name"]);
                        return;
                    }
                }
                else
                {
                    if (mode.ToLower() == "std" || mode.ToLower() == "standard")
                        Mode = OsuGameMode.Standard;
                    else if (mode.ToLower() == "taiko")
                        Mode = OsuGameMode.Taiko;
                    else if (mode.ToLower() == "mania")
                        Mode = OsuGameMode.Mania;
                    else if (mode.ToLower() == "ctb" || mode.ToLower() == "catchthebeat")
                        Mode = OsuGameMode.CatchTheBeat;
                    else
                        player = mode + " " + player; //Maybe no mode was set and the player has a space in the name /shrug
                }

                //Get user info
                OsuService service = new OsuService();
                var Player = service.GetPlayer(player, Mode);
                if(Player == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["osu_404"]);
                    return;
                }
                else if (Player.GlobalRank == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["osu_mode_empty", "NAME", Player.Name, "MODE", Mode]);
                    return;
                }
                string Username = $"{Player.Name} ({Mode})";


                string level = double.Parse(Player.Level).ToString("##0.00");
                string Level = level.Split('.')[0];
                string Percentage = level.Split('.')[1];

                //Send
                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .WithThumbnailUrl(service.GetUserAvatar(Player.Id))
                    .WithTitle(Username)
                    .AddField(lang["osu_country"], $":flag_{Player.Country.ToLower()}:")
                    .AddField(lang["osu_rank"], lang["osu_ranking", "GLOBAL", Player.GlobalRank, "COUNTRY", Player.CountryRank])
                    .AddField(lang["osu_total"], lang["osu_total_", "SCORE", Player.TotalScore, "PLAYS", Player.PlayCount])
                    .AddField(lang["osu_level"], $"{Level} ({Percentage}%)", true)
                    .AddField(lang["osu_pp"], $"{Player.PP}", true)
                    .AddField(lang["osu_acc"], $"{Player.Accuracy?.Truncate(5)}%")
                    .AddField(lang["osu_hits"], $"**300**: {Player.Count300}\n**100**: {Player.Count100}\n**50**: {Player.Count50}", true)
                    .AddField(lang["osu_ranks"], $">**SS+**: {Player.SSPlusCount}\n" +
                                                 $">**SS**: {Player.SSCount}\n" +
                                                 $">**S+**: {Player.SPlusCount}\n" +
                                                 $">**S**: {Player.SCount}\n" +
                                                 $">**A**: {Player.ACount}", true)
                    .WithFooter(lang["osu_powered"]));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Osu", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Osu", null, ex), Context);
            }
        }
    }
}