using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Image
{
    public class KurumiApi : ModuleBase
    {
        //I wish there was a way to get the command name
        [Command("hug")]
        public async Task Hug([Remainder, Optional]string _) => await SendImage(ImageType.Hug);
        [Command("cry")]
        public async Task Cry([Remainder, Optional]string _) => await SendImage(ImageType.Cry);
        [Command("kick")]
        public async Task Kick([Remainder, Optional]string _) => await SendImage(ImageType.Kick);
        [Command("kiss")]
        public async Task Kiss([Remainder, Optional]string _) => await SendImage(ImageType.Kiss);
        [Command("kurumi")]
        public async Task Kurumi([Remainder, Optional]string _) => await SendImage(ImageType.Kurumi);
        [Command("lick")]
        public async Task Lick([Remainder, Optional]string _) => await SendImage(ImageType.Lick);
        [Command("pat")]
        public async Task Pat([Remainder, Optional]string _) => await SendImage(ImageType.Pat);
        [Command("poke")]
        public async Task Poke([Remainder, Optional]string _) => await SendImage(ImageType.Poke);
        [Command("slap")]
        public async Task Slap ([Remainder, Optional]string _) => await SendImage(ImageType.Slap);
        [Command("stare")]
        public async Task Stare([Remainder, Optional]string _) => await SendImage(ImageType.Stare);
        [Command("triggered")]
        public async Task Triggered([Remainder, Optional]string _) => await SendImage(ImageType.Triggered);


        private async Task SendImage(ImageType type)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                string ImageUrl = await SendImageRequest(type);
                await Context.Channel.SendEmbedAsync($"[{lang["image_open"]}]({ImageUrl})", ImageUrl: ImageUrl, Footer: lang["image_powered"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, $"{type}", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Info, $"{type}", null, ex), Context);
            }
        }

        private async Task<string> SendImageRequest(ImageType type)
        {
            string RequestUrl = (Config.SSLEnabled ? "https" : "http") + $"://api.kurumibot.moe/api/image/{type}";
            var response = await new HttpClient().GetStringAsync(RequestUrl).ConfigureAwait(false);
            if (!Config.SSLEnabled)
                response = response.Replace("https://", "http://");
            return response;
        }

        public enum ImageType : byte
        {
            Awoo,
            Cry,
            Hug,
            Kick,
            Kiss,
            Kurumi,
            Lick,
            Pat,
            Slap,
            Stare,
            Triggered,
            Poke
        }
    }
}