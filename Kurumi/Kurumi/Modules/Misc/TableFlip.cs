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
    public class Tableflip : ModuleBase
    {
        [Command("tableflip")]
        public async Task TableFlip([Optional, Remainder]string OwO)
        {
            try
            {
                await Context.Channel.SendEmbedAsync("​(╯°□°）╯︵ ┻━┻");
                await Utilities.Log(new LogMessage(LogSeverity.Info, "TableFlip", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "TableFlip", null, ex), Context);
            }
        }

        [Command("doubleflip")]
        public async Task DoubleFlip([Optional, Remainder]string OwO)
        {
            try
            {
                await Context.Channel.SendEmbedAsync("┻━┻彡 ヽ(ಠДಠ)ノ彡┻━┻");
                await Utilities.Log(new LogMessage(LogSeverity.Info, "DoubleFlip", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "DoubleFlip", null, ex), Context);
            }
        }
    }
}