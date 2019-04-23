using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Misc
{
    public class Ping : ModuleBase
    {
        [Command("ping")]
        public async Task GetPing()
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                IUserMessage msg = (await Context.Channel.SendEmbedAsync("Ping!").ConfigureAwait(false)).Message;
                sw.Stop();
                var embed = new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .WithDescription($"Ping: {sw.ElapsedMilliseconds}ms");
                await msg.ModifyAsync(x => x.Embed = embed.Build());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Ping", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Ping", null, ex), Context);
            }
        }
    }
}