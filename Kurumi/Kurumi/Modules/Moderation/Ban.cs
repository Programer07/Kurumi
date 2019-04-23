using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Moderation
{
    public class Ban : ModuleBase
    {
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser([Optional]string user, [Optional, Remainder]string Reason)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                //No user
                if (user == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_404"]);
                    return;
                }
                //Get user
                IUser TargetUser = await Utilities.GetUser(Context.Guild, user);

                //User not found
                if (TargetUser == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_404"]);
                    return;
                }

                //REEE
                if (TargetUser.Id == Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_invalid_target"]);
                    return;
                }

                //Set reason if null
                if (Reason == null)
                    Reason = lang["ban_no_reason"];

                //Check if Kurumi can ban
                int KurumiHierarchy = (Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id).Result as SocketGuildUser).Hierarchy;
                int TargetHierarchy = (TargetUser as SocketGuildUser).Hierarchy;

                //User is in higher role
                if (TargetHierarchy >= KurumiHierarchy)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_low_role"]);
                    return;
                }

                //Send message
                await Context.Channel.SendEmbedAsync(lang["ban_desc", "SENDER", Context.User.Mention, "REASON", Reason], Title: lang["ban_title", "USER", TargetUser.Username]);

                //Try sending message to target
                try
                {
                    await TargetUser.SendEmbedAsync(lang["ban_user", "GUILD", Context.Guild.Name, "REASON", Reason]);
                }
                catch (Exception) { } //If failes the user has direct messages disabled from the server or is a bot

                //Ban
                await Context.Guild.AddBanAsync(TargetUser, reason: Reason + " Banned by: " + Context.User.ToString());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Ban", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Ban", null, ex), Context);
            }
        }

        [Command("pruneban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser([Optional]string user, [Optional]string days, [Optional, Remainder]string Reason)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                //Parse days
                if (!int.TryParse(days, out int Days))
                {
                    await Context.Channel.SendEmbedAsync(lang["pruneban_invalid_days"]);
                    return;
                }
                if (Days > 7 || Days < 1)
                {
                    await Context.Channel.SendEmbedAsync(lang["pruneban_bad_number"]);
                    return;
                }

                //Get user
                IUser TargetUser = await Utilities.GetUser(Context.Guild, user);
                //User not found
                if (TargetUser == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_404"]);
                    return;
                }

                //REEE
                if (TargetUser.Id == Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_invalid_target"]);
                    return;
                }

                //Set reason if null
                if (Reason == null)
                    Reason = lang["ban_no_reason"];

                //Check if Kurumi can ban
                int KurumiHierarchy = (Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id).Result as SocketGuildUser).Hierarchy;
                int TargetHierarchy = (TargetUser as SocketGuildUser).Hierarchy;

                //User is in higher role
                if (TargetHierarchy >= KurumiHierarchy)
                {
                    await Context.Channel.SendEmbedAsync(lang["ban_low_role"]);
                    return;
                }

                //Send message
                await Context.Channel.SendEmbedAsync(lang["pruneban_desc", "SENDER", Context.User.Mention, "REASON", Reason, "DAYS", days], Title: lang["ban_title", "USER", TargetUser.Username]);

                //Try sending message to target
                try
                {
                    await TargetUser.SendEmbedAsync(lang["ban_user", "GUILD", Context.Guild.Name, "REASON", Reason]);
                }
                catch (Exception) { } //If failes the user has direct messages disabled from the server or is a bot

                //Ban
                await Context.Guild.AddBanAsync(TargetUser, Days, Reason + " Banned by: " + Context.User.ToString());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "PruneBan", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "PruneBan", null, ex), Context);
            }
        }
    }
}