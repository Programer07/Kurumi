using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using Kurumi.Services.Leveling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class Settings : ModuleBase
    {
        [Command("language")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetLanguage([Optional, Remainder]string NewLang)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (NewLang == null)
                {
                    //Create embed
                    EmbedBuilder embed = new EmbedBuilder().WithColor(Config.EmbedColor);
                    embed.WithDescription(lang["language_desc"]);
                    embed.AddField(lang["language_current"], GuildConfigDatabase.GetOrFake(Context.Guild.Id).Lang);

                    string Names = string.Empty;
                    string Versions = string.Empty;
                    string CreatedBy = string.Empty;
                    foreach (var (Data, Lang) in Language.Languages.Values)
                    {
                        var data = Data;
                        Names += $"{data.DisplayName} ({data.Code})\n";
                        Versions += $"{data.Version}\n";
                        CreatedBy += $"{data.CreatedBy}\n";
                    }


                    //Add languages
                    embed.AddField(lang["language_lang"], Names, true);
                    embed.AddField(lang["language_version"], Versions, true);
                    embed.AddField(lang["language_createdby"], CreatedBy, true);
                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
                else if (Language.GetLanguagCode(NewLang) != null)
                {
                    //Set the language
                    GuildConfigDatabase.GetOrCreate(Context.Guild.Id).Lang = Language.GetLanguagCode(NewLang);
                    //Refresh
                    lang = Language.GetLanguage(Context.Guild);
                    //Send confirmation
                    await Context.Channel.SendEmbedAsync(lang["language_success_set", "LANG", NewLang]);
                }
                else
                    //Send not found
                    await Context.Channel.SendEmbedAsync(lang["language_not_found"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "SetLanguage", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "SetLanguage", null, ex), Context);
            }
        }

        [Command("prefix")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Prefix([Optional, Remainder]string NewPrefix)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (NewPrefix == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["prefix_current"]);
                }
                else
                {
                    if (NewPrefix.Length > 10)
                    {
                        await Context.Channel.SendEmbedAsync(lang["prefix_too_long"]);
                        return;
                    }
                    GuildConfigDatabase.GetOrCreate(Context.Guild.Id).Prefix = NewPrefix;
                    await Context.Channel.SendEmbedAsync(lang["prefix_set"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Prefix", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Prefix", null, ex), Context);
            }
        }

        [Command("ranking")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetRankingCommand([Optional, Remainder]string arg)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (uint.TryParse(arg, out uint Inc)) //Check for increment
                {
                    //Check if valid
                    if (Inc < 5 || Inc > 400)
                    {
                        await Context.Channel.SendEmbedAsync(lang["ranking_out_of_range"]);
                        return;
                    }
                    //Set
                    GuildConfigDatabase.GetOrCreate(Context.Guild.Id).Inc = (int)Inc;
                    //Calculate first 10 levels
                    string Levels = string.Empty;
                    for (uint i = 1; i <= 10; i++)
                        Levels += $"{i,4}. | {ExpManager.LevelStartExp(i, Inc)} - {ExpManager.LevelStartExp(i + 1, Inc) - 1} Exp\n";
                    //Send
                    await Context.Channel.SendEmbedAsync(lang["ranking_inc_set", "INC", Inc, "LEVELS", Levels]);
                }
                else if (bool.TryParse(arg, out bool State)) //Check for bool
                {
                    //Set, send
                    GuildConfigDatabase.GetOrCreate(Context.Guild.Id).Ranking = State;
                    await Context.Channel.SendEmbedAsync(lang["ranking_state_set", "STATE", State]);
                }
                else //Send current settings
                {
                    var current = GuildConfigDatabase.GetOrFake(Context.Guild.Id);
                    int inc = current.Inc;
                    bool state = current.Ranking;
                    await Context.Channel.SendEmbedAsync(lang["ranking_state", "INC", inc, "STATE", state]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "SetRanking", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "SetRanking", null, ex), Context);
            }
        }

        [Command("welcomechannel")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetWelcomeChannelCommand([Optional, Remainder]string NewChannel)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                if (NewChannel == null)
                {
                    //Get current channel
                    ulong currentchannel = GuildConfigDatabase.GetOrFake(Context.Guild.Id).WelcomeChannel;
                    //Check if the channel is empty
                    if (currentchannel == 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomechannel_no"]);
                        return;
                    }
                    //Check if valid
                    IGuildChannel channel = Context.Guild.GetChannelAsync(currentchannel).Result;
                    if (channel == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomechannel_invalid"]);
                        return;
                    }
                    //Send
                    await Context.Channel.SendEmbedAsync(lang["welcomechannel_channel", "CHANNEL", channel.Name]);
                }
                else if (NewChannel == "remove")
                {
                    //Set to 0 and send
                    var GuildConfig = GuildConfigDatabase.Get(Context.Guild.Id);
                    if (GuildConfig != null)
                        GuildConfig.WelcomeChannel = 0;
                    await Context.Channel.SendEmbedAsync(lang["welcomechannel_removed"]);
                }
                else
                {
                    //Try checking for id
                    IGuildChannel channel = await Utilities.GetChannel(Context.Guild, NewChannel);
                    //Check if found
                    if (channel == null || !(channel is ITextChannel))
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomechannel_invalid_channel"]);
                        return;
                    }
                    //Set, add default message and send
                    var guildConfig = GuildConfigDatabase.GetOrCreate(Context.Guild.Id);
                    guildConfig.WelcomeChannel = channel.Id;
                    guildConfig.WelcomeMessages.Add("Welcome {user} to the {server} server!");
                    await Context.Channel.SendEmbedAsync(lang["welcomechannel_set", "CHANNEL", channel.Name]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "SetWelcomeChannel", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Info, "SetWelcomeChannel", null, ex), Context);
            }
        }

        [Command("welcomemessage")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task WelcomeMessage([Optional]string op, [Optional, Remainder]string arg)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                op = op?.ToLower();
                if (op == "add")
                {
                    //Check if there is a welcome channel
                    ulong wchannel = GuildConfigDatabase.GetOrFake(Context.Guild.Id).WelcomeChannel;
                    if (wchannel == 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_no_channel"]);
                        return;
                    }
                    //Check if the message is valid
                    if (arg == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_no_message"]);
                        return;
                    }
                    //Check length
                    if (arg.Length > 1900)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_message_too_long"]);
                        return;
                    }
                    if (GuildConfigDatabase.Get(Context.Guild.Id).WelcomeMessages.Count == 5 && DiscordBotlist.UserVoted(Context.Guild.OwnerId).Result)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_too_much", "OWNER", Context.Guild.GetOwnerAsync().Result.Username]);
                        return;
                    }
                    //Add it
                    GuildConfigDatabase.Get(Context.Guild.Id).WelcomeMessages.Add(arg);
                    await Context.Channel.SendEmbedAsync(lang["welcomemessage_set"]);
                }
                else if (op == "remove")
                {
                    //Check if Id is valid
                    if (arg == null || !int.TryParse(arg, out int Id))
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_invalid_id"]);
                        return;
                    }
                    //Check if Id is a welcome message or not removing last
                    List<string> msges = GuildConfigDatabase.GetOrFake(Context.Guild.Id)?.WelcomeMessages;
                    if (msges.Count == 1)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_cannot_remove"]);
                        return;
                    }
                    else if (msges.Count < Id)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_doesnt_exists"]);
                        return;
                    }
                    //Remove and send
                    GuildConfigDatabase.Get(Context.Guild.Id).WelcomeMessages.RemoveAt(Id - 1);
                    await Context.Channel.SendEmbedAsync(lang["welcomemessage_removed"]);
                }
                else if (op == "list")
                {
                    //Get page id
                    if (!int.TryParse(arg, out int page))
                        page = 1;
                    page--;
                    //Get all
                    List<string> msges = GuildConfigDatabase.GetOrFake(Context.Guild.Id).WelcomeMessages;
                    //Construct string
                    int i = 1;
                    string PageMessages = string.Empty;
                    string allMessage = string.Empty;
                    foreach (string msg in msges)
                    {
                        //Start from the length
                        if (page * 2000 > allMessage.Length + msg.Length + 5)
                        {
                            allMessage += $"\n{i++}) " + msg;
                            continue;
                        }
                        //Go till the page limit is hit
                        if (PageMessages.Length + msg.Length + 5 < 2000)
                            PageMessages += $"\n{i++}) " + msg;
                    }
                    //Check if page exists
                    if (PageMessages == string.Empty)
                    {
                        await Context.Channel.SendEmbedAsync(lang["welcomemessage_page_doesnt_exists"]);
                        return;
                    }
                    //Send
                    await Context.Channel.SendEmbedAsync(PageMessages, Title: lang["welcomemessage_messages", "PAGE", $"{page + 1}/{Math.Ceiling((double)(allMessage.Length + PageMessages.Length) / 2000)}"]);
                }
                else
                {
                    //Invalid operation
                    await Context.Channel.SendEmbedAsync(lang["welcomemessage_bad_syntax"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ManageWelcomeMessages", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ManageWelcomeMessages", null, ex), Context);
            }
        }

        [Command("levelreward")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task LevelRewardCommand([Optional]string op, [Optional]string level, [Optional, Remainder]string role)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                op = op?.ToLower();
                if (op == "list")
                {
                    //Check if there are any
                    Reward rewards = RewardDatabase.Get(Context.Guild.Id);
                    if (rewards == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_empty"]);
                        return;
                    }
                    //Load
                    if (rewards.Rewards.Count == 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_empty"]);
                        return;
                    }
                    //Construct the string
                    string RoleList = string.Empty;
                    foreach (KeyValuePair<int, ulong> a in rewards.Rewards)
                    {
                        IRole Role = Context.Guild.GetRole(a.Value);
                        RoleList += lang["levelreward_list_item", "LEVEL", a.Key.ToString(), "ROLE", Role?.Name ?? "INVALID-ROLE"];
                    }
                    //Send
                    await Context.Channel.SendEmbedAsync(lang["levelreward_desc", "LIST", RoleList]);
                }
                else if (op == "add" || op == "set")
                {
                    //Try to get the target level
                    if (!int.TryParse(level, out int Level) && Level <= 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_invalid_level"]);
                        return;
                    }
                    Reward rewards = RewardDatabase.GetOrCreate(Context.Guild.Id);
                    //Check if the level has a role set already
                    if (rewards.Rewards.ContainsKey(Level))
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_already_set_level"]);
                        return;
                    }
                    //Get role
                    IRole Role = null;
                    if (role != null)
                    {
                        Role = Utilities.GetRole(Context.Guild, role);
                        if (Role == null)
                        {
                            await Context.Channel.SendEmbedAsync(lang["levelreward_invalid_role"]);
                            return;
                        }
                    }
                    else
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_invalid_role"]);
                        return;
                    }
                    //Check if the role is already set
                    if (rewards.Rewards.ContainsValue(Role.Id))
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_already_set_role"]);
                        return;
                    }
                    //Add
                    rewards.Rewards.Add(Level, Role.Id);
                    await Context.Channel.SendEmbedAsync(lang["levelreward_success_set", "LEVEL", level, "ROLE", Role.Name]);
                }
                else if (op == "remove")
                {

                    //Try to get the target level
                    if (!int.TryParse(level, out int Level) && Level <= 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_invalid_level"]);
                        return;
                    }
                    //Get levels
                    Reward rewards = RewardDatabase.Get(Context.Guild.Id);
                    //Check if there are any
                    if (rewards == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_empty"]);
                        return;
                    }

                    //Check if the level has a role set already
                    if (!rewards.Rewards.ContainsKey(Level))
                    {
                        await Context.Channel.SendEmbedAsync(lang["levelreward_not_set"]);
                        return;
                    }
                    //Remove
                    rewards.Rewards.Remove(Level);
                    await Context.Channel.SendEmbedAsync(lang["levelreward_success_remove", "LEVEL", level]);
                }
                else
                {
                    //Invalid operation
                    await Context.Channel.SendEmbedAsync(lang["levelreward_bad_syntax"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "SetLevelReward", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "SetLevelReward", null, ex), Context);
            }
        }
    }
}