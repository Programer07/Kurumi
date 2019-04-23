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

namespace Kurumi.Modules.Utility
{
    public class ChannelName : ModuleBase
    {
        [Command("ChannelName")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        public async Task SetChannelName([Optional]string Channel, [Remainder, Optional]string Name)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                //Check if everything is entered and correct
                if (Channel == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelname_not_found"]);
                    return;
                }
                if (Name == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelname_name_empty"]);
                    return;
                }
                if (Name.Length > 100)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelname_too_long"]);
                    return;
                }
                //Get channel
                IGuildChannel channel = Context.Guild.GetChannelsAsync().Result.FirstOrDefault(x =>
                    x.Name.Equals(Channel, StringComparison.CurrentCultureIgnoreCase) || x.Id.ToString() == Channel || (x as ITextChannel)?.Mention == Channel
                );
                //Check if success
                if (channel == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelname_not_found"]);
                    return;
                }
                //Re name
                await channel.ModifyAsync(x => x.Name = Name);
                //Send
                await Context.Channel.SendEmbedAsync(lang["channelname_success", "NAME", Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ChannelName", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ChannelName", null, ex), Context);
            }
        }
    }
}
