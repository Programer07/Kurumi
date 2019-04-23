using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class ServerInfo : ModuleBase
    {
        [Command("serverinfo")]
        [Alias("si")]
        [RequireContext(ContextType.Guild)]
        public async Task SendServerInfo()
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

                await Utilities.Log(new LogMessage(LogSeverity.Info, "ServerInfo", "success-exit-0"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ServerInfo", null, ex), Context);
            }
        }
    }
}