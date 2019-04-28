using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Moderation
{
    public class WhoSaidThis : ModuleBase
    {
        [Command("whosaidthis")]
        [Alias("wst")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task CheckWhoSaidThis([Optional, Remainder]string Input)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var guild = GuildDatabase.GetOrFake(Context.Guild.Id);
                DeletedMessage msg;
                if (string.IsNullOrWhiteSpace(Input) || (msg = guild.Messages.FirstOrDefault(x => x.MessageId.ToString() == Input)) == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["whosaidthis_message_404"]);
                    return;
                }
                await Context.Channel.SendEmbedAsync($"{lang["whosaidthis_text"]}{msg.Text.Unmention()}\n{lang["whosaidthis_user"]}{msg.SentBy.Unmention()}\n{lang["whosaidthis_time"]}{msg.SentAt.ToLongDateString()}");
                await Utilities.Log(new LogMessage(LogSeverity.Info, "WhoSaidThis", "success"));
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "WhoSaidThis", null, ex));
            }
        }
    }
}