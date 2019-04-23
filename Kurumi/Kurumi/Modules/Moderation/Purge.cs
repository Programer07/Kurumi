using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Moderation
{
    public class Purge : ModuleBase
    {
        [Command("purge")]
        [Alias("clear")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task PurgeCommand([Optional, Remainder]string In)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                //Check if valid
                if (!int.TryParse(In, out int Messages))
                {
                    await Context.Channel.SendEmbedAsync(lang["purge_not_number"]);
                    return;
                }
                //Check if larger then limit
                if (Messages > 200)
                {
                    await Context.Channel.SendEmbedAsync(lang["purge_too_much"]);
                    return;
                }
                //Select messages
                IEnumerable<IMessage> SelectedMessages = await Context.Channel.GetMessagesAsync(Messages).FlattenAsync().ConfigureAwait(false);
                //Remove messages older then 2 weeks and pinned messages
                SelectedMessages = SelectedMessages.Where(x => (DateTime.Now - x.CreatedAt).Days < 14 && !x.IsPinned);
                //Delet
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(SelectedMessages);
                //Send
                await Context.Channel.SendEmbedAsync(lang["purge_done", "COUNT", SelectedMessages.Count(), "USER", Context.User.Username]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Purge", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Purge", null, ex), Context);
            }
        }
    }
}