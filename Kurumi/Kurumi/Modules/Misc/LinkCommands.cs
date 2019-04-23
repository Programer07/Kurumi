using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Misc
{
    public class LinkCommands : ModuleBase
    {
        [Command("support")]
        public async Task Support()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                await Context.User.SendEmbedAsync(lang["linkcommand_support_join"]);
                await Context.Channel.SendEmbedAsync(lang["linkcommand_in_dm"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Support", "success"));
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Support", null, ex), Context);
            }
        }

        [Command("donate")]
        public async Task Donate()
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync($"[{lang["linkcommand_click_senpai"]}]({"https://www.patreon.com/kurumibot0"})!", "Patreon");
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Donate", "success"));
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Donate", null, ex), Context);
            }
        }

        [Command("invite")]
        public async Task Invite()
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync($"[{lang["linkcommand_click_senpai"]}]({"https://discordapp.com/oauth2/authorize?client_id=374274129282596885&scope=bot&permissions=8"})!", lang["linkcommand_invite_link"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Invite", "success"));
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Invite", null, ex), Context);
            }
        }

        [Command("vote")]
        public async Task Vote()
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync($"[{lang["linkcommand_click_senpai"]}]({"https://discordbots.org/bot/374274129282596885"})!", lang["linkcommand_vote_link"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Invite", "success"));
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Invite", null, ex), Context);
            }
        }
    }
}
