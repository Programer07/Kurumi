using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Permission;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Misc
{
    public class Help : ModuleBase
    {
        [Command("help")]
        [Alias("h")]
        public async Task HelpCommand([Optional, Remainder]string command)
        {
            try
            {
                List<string> Categories = new List<string>();
                for (int i = 0; i < CommandData.CommandCache.Count; i++)
                {
                    var cmd = CommandData.CommandCache[i];
                    if (!Categories.Contains(cmd.Category))
                        Categories.Add(cmd.Category);
                }


                var lang = Language.GetLanguage(Context.Guild);
                if (command == null) //Print list of categories
                {
                    Categories.Sort();
                    IApplication application = await Context.Client.GetApplicationInfoAsync();
                    await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                        .WithColor(Config.EmbedColor)
                        .WithThumbnailUrl(application.IconUrl)
                        .AddField(lang["help_title"],
                        lang["help_main_description", "CAT_COUNT", Categories.Count]
                        + $"   •{string.Join("\n  •", Categories)}"));
                }
                else if (CommandData.CategoryExists(command)) //Print commands from category
                {
                    var commands = CommandData.GetCommandsFor(command);
                    await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                        .WithColor(Config.EmbedColor)
                        .AddField(lang["help_category", "CATEGORY", command],
                            $"\n  •{string.Join(Environment.NewLine + "  •", commands.ConvertAll(x => x.CommandName))}"));
                }
                else if (PermissionManager.NameOfCommand(command)) //Print command info
                {
                    var cmd = CommandData.GetCommand(command);
                    string prefix = GuildDatabase.GetOrFake(Context.Guild.Id).Prefix;
                    await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                        .WithColor(Config.EmbedColor)
                        .WithDescription($"**[**``{cmd.CommandName}``**]**{Environment.NewLine}" +
                            $"{lang["help_usage"]}{cmd.Usage.Replace("@PREFIX@", prefix)}{Environment.NewLine}" +
                            $"{lang["help_alias"]}{cmd.Alias.Replace("@PREFIX@", prefix)}{Environment.NewLine}" +
                            $"{lang["help_description"]}{cmd.Description.Replace("@PREFIX@", prefix)}"));
                }
                else //Print not found
                    await Context.Channel.SendEmbedAsync(lang["help_not_found"]);

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Help", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Help", null, ex), Context);
            }
        }
    }
}