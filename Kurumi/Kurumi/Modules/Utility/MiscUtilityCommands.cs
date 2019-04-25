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
    public class MiscUtilityCommands : ModuleBase
    {
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

        [Command("userid")]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Optional, Remainder]string User)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var user = await Utilities.GetUser(Context.Guild, User);
                if (user == null)
                    await Context.Channel.SendEmbedAsync(lang["util_user_not_found"]);
                else
                    await Context.Channel.SendEmbedAsync(lang["util_userid", "USER", user.Username, "ID", user.Id]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "UserId", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "UserId", null, ex), Context);
            }
        }

        [Command("channelid")]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelId([Optional, Remainder]string Channel)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var channel = await Utilities.GetChannel(Context.Guild, Channel);
                if (channel == null)
                    await Context.Channel.SendEmbedAsync(lang["util_channel_not_found"]);
                else
                    await Context.Channel.SendEmbedAsync(lang["util_channelid", "CHANNEL", channel.Name, "ID", channel.Id]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ChannelId", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ChannelId", null, ex), Context);
            }
        }

        [Command("roleid")]
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

        [Command("avatar")]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Remainder, Optional]string user)
        {
            try
            {
                IUser User;
                if (user == null)
                    User = Context.User;
                else
                    User = await Utilities.GetUser(Context.Guild, user);
                var lang = Language.GetLanguage(Context.Guild);
                if (User == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["util_user_not_found"]);
                    return;
                }
                await Context.Channel.SendEmbedAsync(null, Title: lang["avatar_user", "USER", User], ImageUrl: User.GetAvatarUrl(size: 256));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Avatar", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Avatar", null, ex), Context);
            }
        }

        [Command("servericon")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerIcon()
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
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
    }
}