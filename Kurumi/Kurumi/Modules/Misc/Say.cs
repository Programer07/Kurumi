using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Misc
{
    public class Say : ModuleBase
    {
        [Command("say")]
        public async Task SayCommand([Remainder, Optional]string text)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (text == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["say_no_text"]);
                    return;
                }
                string title = null;
                if (text.Contains("{title="))
                {
                    string[] cont = text.Split("{title=");
                    if (cont[1].EndsWith("}"))
                        title = cont[1].Remove("}");
                }
                await Context.Channel.SendEmbedAsync(title == null ? text.Unmention() : text.Split("{title=")[0].Unmention(), title);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Say", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Say", null, ex), Context);
            }
        }

        [Command("sayc")]
        [Alias("sayd")]
        [RequireContext(ContextType.Guild)]
        public async Task SaycCommand([Optional, Remainder]string Text)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                if (Text == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["say_no_text"]);
                    return;
                }
                Message msg = new Message
                {
                    Text = Text,
                    SentAt = DateTime.Now,
                    SentBy = Context.User.ToString()
                };
                string JsonText = JsonConvert.SerializeObject(msg);

                IUserMessage umsg = (await Context.Channel.SendEmbedAsync(Text.Unmention()).ConfigureAwait(false)).Message;

                string Path = $"{KurumiPathConfig.GuildDatabase}{Context.Guild.Id}{KurumiPathConfig.Separator}Messages";
                Directory.CreateDirectory(Path);
                File.WriteAllText(Path + $"{KurumiPathConfig.Separator}{umsg.Id}.json", JsonText);

                await Context.Message.DeleteAsync();
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Sayc", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Sayc", null, ex), Context);
            }
        }

        public class Message
        {
            public string Text { get; set; }
            public DateTime SentAt { get; set; }
            public string SentBy { get; set; }
        }
    }
}