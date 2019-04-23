using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Misc
{
    public class Lenny : ModuleBase
    {
        [Command("lenny")]
        public async Task SendLenny([Optional, Remainder]string OwO)
        {
            try
            {
                await Context.Channel.SendEmbedAsync("( ͡° ͜ʖ ͡°)");
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lenny", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lenny", null, ex), Context);
            }
        }
    }
}