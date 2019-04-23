using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Moderation
{
    public class Warnings : ModuleBase
    {
        [Command("warn")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Warn([Optional, Remainder]string user)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                IUser TargetUser = await Utilities.GetUser(Context.Guild, user);

                if(TargetUser == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_not_found"]);
                    return;
                }
                else if (Context.Guild.OwnerId == TargetUser.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_owner_target"]);
                    return;
                }
                else if (Context.User.Id == TargetUser.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_invalid"]);
                    return;
                }

                var GuildWarnings = ModDatabase.GetOrCreate(Context.Guild.Id);
                var UserWarning = GuildWarnings.FirstOrDefault(x => x.UserId == TargetUser.Id);
                if(UserWarning == null)
                {
                    var w = new Services.Database.Models.UserWarning() { Count = 0, UserId = TargetUser.Id };
                    UserWarning = w;
                    GuildWarnings.Add(w);
                }
                UserWarning.Count++;
                if(UserWarning.Count == GuildConfigDatabase.GetOrCreate(Context.Guild.Id).MaxWarnings)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_too_many_warnings", "TARGET", TargetUser.Username]);
                    GuildWarnings.Remove(UserWarning);
                    await Execute(Context, TargetUser);
                }
                else
                    await Context.Channel.SendEmbedAsync(lang["automod_newwarning", "TARGET", TargetUser.Username, "COUNT", UserWarning.Count, "MAX", GuildConfigDatabase.Get(Context.Guild.Id).MaxWarnings]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Warn", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Warn", null, ex), Context);
            }
        }
        [Command("removewarning")]
        [Alias("rwarning")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task RemoveWarning([Optional, Remainder]string user)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                IUser TargetUser = await Utilities.GetUser(Context.Guild, user);
                if(TargetUser == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_not_found"]);
                    return;
                }
                var GuildWarnings = ModDatabase.GetOrCreate(Context.Guild.Id);
                var UserWarning = GuildWarnings.FirstOrDefault(x => x.UserId == TargetUser.Id);
                if(UserWarning == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_no_warnings", "TARGET", TargetUser.Username]);
                    return;
                }
                UserWarning.Count--;
                if (UserWarning.Count == 0)
                    GuildWarnings.Remove(UserWarning);
                await Context.Channel.SendEmbedAsync(lang["automod_removed_warning", "TARGET", TargetUser.Username, "COUNT", UserWarning.Count]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "RemoveWarning", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RemoveWarning", null, ex), Context);
            }
        }
        [Command("warnings")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ListWarning([Optional, Remainder]string user)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                IUser TargetUser = await Utilities.GetUser(Context.Guild, user);
                if (TargetUser == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_not_found"]);
                    return;
                }
                var GuildWarnings = ModDatabase.GetOrCreate(Context.Guild.Id);
                var UserWarning = GuildWarnings.FirstOrDefault(x => x.UserId == TargetUser.Id);
                if (UserWarning == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["automod_no_warnings", "TARGET", TargetUser.Username]);
                    return;
                }
                if (UserWarning == null)
                    await Context.Channel.SendEmbedAsync(lang["automod_no_warnings", "TARGET", TargetUser.Username]);
                else
                    await Context.Channel.SendEmbedAsync(lang["automod_warnings", "TARGET", TargetUser.Username, "COUNT", UserWarning.Count]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ListWarning", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ListWarning", null, ex), Context);
            }
        }
        [Command("modsettings")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ModSettings([Optional]string Arg1, [Optional]string Arg2, [Optional, Remainder]string Arg3)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var GuildConfig = GuildConfigDatabase.GetOrCreate(Context.Guild.Id);
                if (Arg1 == null) //modsettings
                {
                    string Words = "\n•" + lang["modsettings_no_words"];
                    if (GuildConfig.BlakclistedWords.Count > 0)
                        Words = "\n•" + string.Join("\n•", GuildConfig.BlakclistedWords);

                    await Context.Channel.SendEmbedAsync(lang["modsettings_settings", "MAXWAR", GuildConfig.MaxWarnings, "WORDPUNISHMENT", GuildConfig.PunishmentForWord, "WARNINGPUNISHMENT", GuildConfig.PunishmentForWarning,
                                                                              "BLACKLISTEDWORDS", Words]);
                }
                else if (Arg1.ToLower() == "maxwarnings") //modsettings maxwarnings OR modsettings maxwarnings <number>
                {
                    if (Arg2 == null) //modsettings maxwarnings
                    {
                        await Context.Channel.SendEmbedAsync(lang["modsettings_maxwarning", "COUNT", GuildConfig.MaxWarnings]);
                    }
                    else //modsettings maxwarnings <number>
                    {
                        if (!int.TryParse(Arg2, out int NewMaxWarning) || NewMaxWarning > 10 || NewMaxWarning < 2)
                        {
                            await Context.Channel.SendEmbedAsync(lang["modsettings_maxwarning_invalid_number"]);
                        }
                        else
                        {
                            GuildConfig.MaxWarnings = NewMaxWarning;
                            await Context.Channel.SendEmbedAsync(lang["modsettings_maxwarning_set", "COUNT", GuildConfig.MaxWarnings]);
                        }
                    }
                }
                else if (Arg1.ToLower() == "punishmentforword") //modsettings punishmentforword OR modsettings punishmentforword  <warning/delete>
                {
                    if (Arg2 == null) //modsettings punishmentforword
                    {
                        await Context.Channel.SendEmbedAsync(lang["modsettings_punishmentforword", "P", GuildConfig.PunishmentForWord]);
                    }
                    else //modsettings punishmentforword <warning/delete>
                    {
                        if (Arg2.ToLower() == "warning" || Arg2.ToLower() == "delete")
                        {
                            GuildConfig.PunishmentForWord = Arg2;
                            await Context.Channel.SendEmbedAsync(lang["modsettings_punishmentforword_set", "P", GuildConfig.PunishmentForWord]);
                        }
                        else
                        {
                            await Context.Channel.SendEmbedAsync(lang["modsettings_punishmentforword_invalid"]);
                        }
                    }
                }
                else if (Arg1.ToLower() == "punishmentforwarning") //modsettings punishmentforwarning OR modsettings punishmentforwarning  <kick/ban>
                {
                    if (Arg2 == null) //modsettings punishmentforwarning
                    {
                        await Context.Channel.SendEmbedAsync(lang["modsettings_punishmentforwarning", "P", GuildConfig.PunishmentForWarning]);
                    }
                    else //modsettings punishmentforwarning <kick/ban>
                    {
                        if (Arg2.ToLower() == "kick" || Arg2.ToLower() == "ban")
                        {
                            GuildConfig.PunishmentForWarning = Arg2;
                            await Context.Channel.SendEmbedAsync(lang["modsettings_punishmentforwarning_set", "P", GuildConfig.PunishmentForWarning]);
                        }
                        else
                        {
                            await Context.Channel.SendEmbedAsync(lang["modsettings_punishmentforwarning_invalid"]);
                        }
                    }
                }
                else if (Arg1.ToLower() == "blacklistedwords")
                {
                    if (Arg2 == null)
                    {
                        string Words = "\n•" + lang["modsettings_no_words"];
                        if (GuildConfig.BlakclistedWords.Count > 0)
                            Words = "\n•" + string.Join("\n•", GuildConfig.BlakclistedWords);

                        await Context.Channel.SendEmbedAsync(lang["modsettings_blacklisted", "WORDS", Words]);
                    }
                    else if (Arg2.ToLower() == "add" && Arg3 != null)
                    {
                        if (GuildConfig.BlakclistedWords.Any(x => x.Equals(Arg3, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            await Context.Channel.SendEmbedAsync(lang["modsettings_blacklisted_contains"]);
                        }
                        else
                        {
                            GuildConfig.BlakclistedWords.Add(Arg3);
                            await Context.Channel.SendEmbedAsync(lang["modsettings_blacklisted_added"]);
                        }
                    }
                    else if (Arg2.ToLower() == "remove" && Arg3 != null)
                    {
                        if (!GuildConfig.BlakclistedWords.Any(x => x.Equals(Arg3, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            await Context.Channel.SendEmbedAsync(lang["modsettings_blacklisted_doesnt_contains"]);
                        }
                        else
                        {
                            GuildConfig.BlakclistedWords.RemoveAll(x => x.Equals(Arg3, StringComparison.CurrentCultureIgnoreCase));
                            await Context.Channel.SendEmbedAsync(lang["modsettings_blacklisted_removed", "WORD", Arg3]);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendEmbedAsync(lang["modsettings_blacklisted_invalid"]);
                    }
                }
                else
                {
                    await Context.Channel.SendEmbedAsync(lang["modsettings_invalid"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ModSettings", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ModSettings", null, ex), Context);
            }
        }


        public static async Task CheckForWord(ICommandContext Context)
        {
            try
            {
                //if (Context.Guild.OwnerId == Context.Message.Author.Id)
                    //return;

                var GuildConfig = GuildConfigDatabase.Get(Context.Guild.Id);
                if (GuildConfig == null)
                    return;

                List<string> BlackListedWords = GuildConfig.BlakclistedWords;
                if (BlackListedWords.Count == 0)
                    return;
                foreach (string word in BlackListedWords)
                {
                    if (Context.Message.Content.Contains(" " + word + " ", StringComparison.CurrentCultureIgnoreCase) || 
                        Context.Message.Content.Contains(word + " ", StringComparison.CurrentCultureIgnoreCase) ||
                        Context.Message.Content.Contains(" " + word, StringComparison.CurrentCultureIgnoreCase) || 
                        Context.Message.Content.Equals(word, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var lang = Language.GetLanguage(Context.Guild);
                        if (GuildConfig.PunishmentForWord.ToLower() == "delete")
                        {
                            await Context.Message.DeleteAsync();
                            await Context.Channel.SendEmbedAsync(lang["automod_blacklisted_word", "USER", Context.Message.Author.Username]);
                        }
                        else
                        {
                            var GuildWarnings = ModDatabase.GetOrCreate(Context.Guild.Id);
                            var UserWarning = GuildWarnings.FirstOrDefault(x => x.UserId == Context.User.Id);
                            if (UserWarning == null)
                            {
                                var w = new Services.Database.Models.UserWarning() { Count = 0, UserId = Context.User.Id };
                                UserWarning = w;
                                GuildWarnings.Add(w);
                            }
                            UserWarning.Count++;
                            if (UserWarning.Count == GuildConfig.MaxWarnings)
                            {
                                await Context.Channel.SendEmbedAsync(lang["automod_too_many_warnings", "TARGET", Context.User.Username]);
                                GuildWarnings.Remove(UserWarning);
                                await Execute(Context, Context.User);
                            }
                            else
                            {
                                await Context.Channel.SendEmbedAsync(lang["automod_blacklisted_word_warning", "USER", Context.Message.Author.Username, 
                                                                    "COUNT", UserWarning.Count, "MAX", GuildConfigDatabase.Get(Context.Guild.Id).MaxWarnings]);
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "CheckWords", null, ex));
            }
        }
        private static async Task Execute(ICommandContext Context, IUser TargetUser)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                string Punishment = GuildConfigDatabase.Get(Context.Guild.Id).PunishmentForWarning.ToLower();
                if (Punishment == "kick")
                {
                    await TargetUser.TrySendEmbedAsync(lang["automod_kick_dm", "GUILD", Context.Guild.Name]);
                    try
                    {
                        await (TargetUser as SocketGuildUser).KickAsync(lang["automod_kick_reason"]);
                    }
                    catch (Exception)
                    {
                        if (Context.User.Id == TargetUser.Id)
                            await (await Context.Guild.GetOwnerAsync()).TrySendEmbedAsync(lang["automod_cant_kick_dm_owner", "USER", TargetUser.ToString()]);
                        else
                            await Context.User.TrySendEmbedAsync(lang["automod_cant_kick_dm", "USER", TargetUser.ToString()]);

                        await Context.Channel.SendEmbedAsync(lang["automod_cant_kick", "USER", TargetUser.ToString()]);
                    }
                }
                else if (Punishment == "ban")
                {
                    await TargetUser.TrySendEmbedAsync(lang["automod_ban_dm", "GUILD", Context.Guild.Name]);
                    try
                    {
                        await Context.Guild.AddBanAsync(TargetUser, reason: lang["automod_ban_reason"]);
                    }
                    catch(Exception)
                    {
                        if (Context.User.Id == TargetUser.Id)
                            await (await Context.Guild.GetOwnerAsync()).TrySendEmbedAsync(lang["automod_cant_ban_dm_owner", "USER", TargetUser.ToString()]);
                        else
                            await Context.User.TrySendEmbedAsync(lang["automod_cant_ban_dm", "USER", TargetUser.ToString()]);

                        await Context.Channel.SendEmbedAsync(lang["automod_cant_ban", "USER", TargetUser.ToString()]);
                    }
                }
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "WarningExecute", null, ex));
            }
        }
    }
}