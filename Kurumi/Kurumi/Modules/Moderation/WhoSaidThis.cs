using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Kurumi.Modules.Misc.Say;

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
                string FilePath = $"{KurumiPathConfig.GuildDatabase}{Context.Guild.Id}{KurumiPathConfig.Separator}Messages{KurumiPathConfig.Separator}{Input}.json";
                if (string.IsNullOrWhiteSpace(Input) || !File.Exists(FilePath))
                {
                    await Context.Channel.SendEmbedAsync(lang["whosaidthis_message_404"]);
                    return;
                }
                string EmbedDesc = lang["whosaidthis_unsupported"];
                try
                {
                    Message msg = JsonConvert.DeserializeObject<Message>(File.ReadAllText(FilePath));
                    EmbedDesc = $"{lang["whosaidthis_text"]}{msg.Text.Unmention()}\n{lang["whosaidthis_user"]}{msg.SentBy.Unmention()}\n{lang["whosaidthis_time"]}{msg.SentAt.ToLongDateString()}";
                }
                catch (Exception) { } //Old style
                await Context.Channel.SendEmbedAsync(EmbedDesc);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "WhoSaidThis", "success"));
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "WhoSaidThis", null, ex));
            }
        }
    }
}