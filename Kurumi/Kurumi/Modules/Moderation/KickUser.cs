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
    public class KickUser : ModuleBase
    {
        [Command("kickuser")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUserCommand([Optional]string user, [Optional, Remainder]string Reason)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                //Get user
                IUser TargetUser = await Utilities.GetUser(Context.Guild, user);
                //User not found
                if (TargetUser == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["kick_404"]);
                    return;
                }

                //REEE
                if (TargetUser.Id == Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["kick_invalid_target"]);
                    return;
                }

                //Set reason if null
                if (Reason == null)
                    Reason = lang["kick_no_reason"];

                //Check if Kurumi can kick
                int KurumiHierarchy = (Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id).Result as SocketGuildUser).Hierarchy;
                int TargetHierarchy = (TargetUser as SocketGuildUser).Hierarchy;

                //User is in higher role
                if (TargetHierarchy >= KurumiHierarchy)
                {
                    await Context.Channel.SendEmbedAsync(lang["kick_low_role"]);
                    return;
                }

                //Send message
                await Context.Channel.SendEmbedAsync(lang["kick_desc", "SENDER", Context.User.Mention, "REASON", Reason], Title: lang["kick_title", "USER", TargetUser.Username]);

                //Try sending message to target
                try
                {
                    await TargetUser.SendEmbedAsync(lang["kick_user", "GUILD", Context.Guild.Name, "REASON", Reason]);
                }
                catch (Exception) { } //If failes the user has direct messages disabled from the server or is a bot

                //Kick
                await (TargetUser as SocketGuildUser).KickAsync(Reason + "Kicked by: " + Context.User.ToString());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "KickUser", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "KickUser", ex.ToString()), Context);
            }
        }
    }
}