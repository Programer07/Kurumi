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
    public class ChannelCommands : ModuleBase
    {
        [Command("channelcategoryname")]
        [Alias("categoryname")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        public async Task SetCategoryName([Optional]string Category, [Remainder, Optional]string Name)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                //Check if everything is entered and correct
                if (Category == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelcategory_not_found"]);
                    return;
                }
                if (Name == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelcategory_name_empty"]);
                    return;
                }
                if (Name.Length > 100)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelcategory_too_long"]);
                    return;
                }
                //Get category
                ICategoryChannel category = Context.Guild.GetCategoriesAsync().Result.FirstOrDefault(x =>
                    x.Name.Equals(Category, StringComparison.CurrentCultureIgnoreCase) || x.Id.ToString() == Category
                );
                //Check if success
                if (category == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["channelcategory_not_found"]);
                    return;
                }
                //Re name
                await category.ModifyAsync(x => x.Name = Name);
                //Send
                await Context.Channel.SendEmbedAsync(lang["channelcategory_success", "NAME", Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ChannelCategoyName", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ChannelCategoryName", null, ex), Context);
            }
        }

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
    }
}