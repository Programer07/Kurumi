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
    public class ChannelCategoryName : ModuleBase
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
    }
}