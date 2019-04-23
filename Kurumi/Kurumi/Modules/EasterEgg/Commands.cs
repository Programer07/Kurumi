using Discord.Commands;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.EasterEgg
{
    public class Commands : ModuleBase
    {
        [Command("lolidance")]
        public async Task Lolidance()
        {
            await Context.Channel.TrySendEmbedAsync("https://loli.dance");
        }

        [Command("eggplant")]
        public async Task Eggplant()
        {
            await Context.Channel.TrySendEmbedAsync(":eggplant:");
        }
    }
}