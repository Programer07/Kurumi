using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class GuildCommands : ModuleBase
    {
        [Command("serverinfo")]
        [Alias("si")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerInfo()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                    .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                    .WithColor(Config.EmbedColor)
                    .WithThumbnailUrl(Context.Guild.IconUrl)
                    .AddField(lang["serverinfo_server_id"], Context.Guild.Id)
                    .AddField(lang["serverinfo_owner"], Context.Guild.GetOwnerAsync().Result)
                    .AddField(lang["serverinfo_createdat"], $"{Context.Guild.CreatedAt.DateTime.ToShortDateString()} " +
                                            $"{Context.Guild.CreatedAt.DateTime.ToShortTimeString()}")
                    .AddField(lang["serverinfo_textchannels"], Context.Guild.GetTextChannelsAsync().Result.Count, true)
                    .AddField(lang["serverinfo_voicechannels"], Context.Guild.GetVoiceChannelsAsync().Result.Count, true)
                    .AddField(lang["serverinfo_members"], Context.Guild.GetUsersAsync().Result.Count, true)
                    .AddField(lang["serverinfo_roles"], $"{Context.Guild.Roles.Count - 1} (+1)", true)
                    .AddField(lang["serverinfo_emojis"], Context.Guild.Emotes.Count, true)
                    .AddField(lang["serverinfo_region"], Context.Guild.VoiceRegionId, true));

                await Utilities.Log(new LogMessage(LogSeverity.Info, "ServerInfo", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ServerInfo", null, ex), Context);
            }
        }

        [Command("servericon")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerIcon()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Context.Guild.IconUrl != null)
                    await Context.Channel.SendEmbedAsync(null, Title: lang["servericon_icon", "GUILD", Context.Guild.Name], ImageUrl: Context.Guild.IconUrl);
                else
                    await Context.Channel.SendEmbedAsync(lang["servericon_empty"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ServerIcon", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ServerIcon", null, ex), Context);
            }
        }

        [Command("emoji")]
        public async Task Emoji([Remainder, Optional]string emoji)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (emoji == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["emoji_invalid"]);
                    return;
                }

                if ((emoji.Contains("<:") || emoji.Contains("<a:")) && emoji.Contains(">"))
                {
                    bool Animated = false;
                    if (emoji.Contains("<a:"))
                        Animated = true;

                    string[] e = emoji.Split(":");
                    if (e.Length == 3)
                    {
                        emoji = e[2].Remove(">");
                        if (ulong.TryParse(emoji, out ulong EmojiId))
                        {
                            await Context.Channel.SendEmbedAsync(null, ImageUrl: $"https://cdn.discordapp.com/emojis/{EmojiId}.{ (Animated ? "gif" : "png")}");
                        }
                        else
                            await Context.Channel.SendEmbedAsync(lang["emoji_invalid"]);
                    }
                    else
                        await Context.Channel.SendEmbedAsync(lang["emoji_invalid"]);
                }
                else
                    await Context.Channel.SendEmbedAsync(lang["emoji_invalid"]); ;
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Emoji", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Emoji", null, ex), Context);
            }
        }

        [Command("serverid")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync(lang["util_serverid", "ID", Context.Guild.Id]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ServerId", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ServerId", null, ex), Context);
            }
        }

        [Command("roleid")] //This should be moved to RoleCommands if I ever make one
        [RequireContext(ContextType.Guild)]
        public async Task RoleId([Optional, Remainder]string Role)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var role = Utilities.GetRole(Context.Guild, Role);
                if (role == null)
                    await Context.Channel.SendEmbedAsync(lang["util_role_not_found"]);
                else
                    await Context.Channel.SendEmbedAsync(lang["util_roleid", "ROLE", role.Name, "ID", role.Id]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "RolelId", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RoleId", null, ex), Context);
            }
        }
    }
}