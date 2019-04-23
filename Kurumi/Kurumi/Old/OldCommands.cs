using Discord.Commands;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Old
{
    public class OldCommands : ModuleBase
    {
        [Command("chess")]
        public async Task Chess([Remainder, Optional]string o)
        {
            await Context.Channel.SendEmbedAsync("This command got removed in the last update. Please use the new lobby command for games.\n" +
                "**To play chess:**\n" +
                "``!k.lobby create chess``\n" +
                "``!k.lobby invite user`` (Kurumi will accept the invite)\n" +
                "``!k.lobby start``");
        }

        [Command("quiz")]
        public async Task Quiz([Remainder, Optional]string o)
        {
            await Context.Channel.SendEmbedAsync("This command got removed in the last update. Please use the new lobby command for games.\n" +
                "``!k.lobby create quiz``\n" +
                "``!k.lobby start``");
        }

        [Command("duel")]
        public async Task Duel([Remainder, Optional]string o)
        {
            await Context.Channel.SendEmbedAsync("This command got removed in the last update. Please use the new lobby command for games.\n" +
                "``!k.lobby create duel``\n" +
                "``!k.lobby invite user`` (Kurumi will accept the invite)\n" +
                "``!k.lobby start``");
        }
    }
}