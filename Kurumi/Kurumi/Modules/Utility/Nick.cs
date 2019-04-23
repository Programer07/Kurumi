using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class Nick : ModuleBase
    {
        [Command("nick")]
        [RequireContext(ContextType.Guild)]
        public async Task ChangeNick([Remainder, Optional]string NickName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (NickName == null || NickName.Length > 32)
                {
                    await Context.Channel.SendEmbedAsync(lang["nick_invalid"]);
                    return;
                }
                try
                {
                    if (NickName.ToLower() == "clear")
                        await (Context.User as IGuildUser).ModifyAsync(x => x.Nickname = Context.User.Username);
                    else
                        await (Context.User as IGuildUser).ModifyAsync(x => x.Nickname = NickName);
                    await Context.Channel.SendEmbedAsync(lang["nick_changed", "NICK", NickName]);
                }
                catch (Exception)
                {
                    await Context.Channel.SendEmbedAsync(lang["nick_no_permission"]);
                } //No permission
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Nick", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Nick", null, ex), Context);
            }
        }
    }
}