using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class AfkMessage : ModuleBase
    {
        [Command("afkmessage")]
        public async Task SetAfkMessage([Optional, Remainder]string Message)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var User = GuildDatabase.GetOrCreate(Context.Guild.Id, Context.User.Id);
                if (Message == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["afkmessage_empty"]);
                    return;
                }
                else if (Message.ToLower() == "remove")
                {
                    if (User.AfkMessage == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["afkmessage_not_set"]);
                        return;
                    }
                    User.AfkMessage = null;
                    await Context.Channel.SendEmbedAsync(lang["afkmessage_removed"]);
                }
                else
                {
                    if (User.AfkMessage != null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["afkmessage_already_set"]);
                        return;
                    }
                    User.AfkMessage = Message.Unmention();
                    await Context.Channel.SendEmbedAsync(lang["afkmessage_set"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "AfkMessage", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "AfkMessage", null, ex), Context);
            }
        }

        public static async Task SendMessage(ICommandContext Context)
        {
            List<ulong> Sent = new List<ulong>();
            foreach (ulong User in Context.Message.MentionedUserIds)
            {
                if (Sent.Contains(User))
                    continue;
                var GuildUser = GuildDatabase.GetOrFake(Context.Guild.Id, User);
                if (GuildUser.AfkMessage != null)
                {
                    var user = await Context.Guild.GetUserAsync(GuildUser.UserId);
                    if (user != null)
                        await Context.Channel.SendEmbedAsync('"' + GuildUser.AfkMessage + $"{'"'} - **{user.Username}**");
                }
                Sent.Add(User);
            }
        }
    }
}