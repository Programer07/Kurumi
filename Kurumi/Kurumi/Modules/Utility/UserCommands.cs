using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services;
using Kurumi.Services.Database.Databases;
using Kurumi.StartUp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class UserCommands : ModuleBase
    {
        [Command("nick")]
        [RequireContext(ContextType.Guild)]
        public async Task ChangeNick([Remainder, Optional]string NickName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (NickName == null || NickName.Length > 32)
                {
                    await Context.Channel.SendEmbedAsync(lang["nick_invalid"]);
                    return;
                }
                try
                {
                    if (NickName.ToLower() == "clear")
                        await (Context.User as IGuildUser).ModifyAsync(x => x.Nickname = Context.User.Username);
                    else
                        await (Context.User as IGuildUser).ModifyAsync(x => x.Nickname = NickName);
                    await Context.Channel.SendEmbedAsync(lang["nick_changed", "NICK", NickName]);
                }
                catch (Exception)
                {
                    await Context.Channel.SendEmbedAsync(lang["nick_no_permission"]);
                } //No permission
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Nick", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Nick", null, ex), Context);
            }
        }

        [Command("avatar")]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Remainder, Optional]string user)
        {
            try
            {
                IUser User;
                if (user == null)
                    User = Context.User;
                else
                    User = await Utilities.GetUser(Context.Guild, user);
                var lang = Language.GetLanguage(Context.Guild);
                if (User == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["util_user_not_found"]);
                    return;
                }
                await Context.Channel.SendEmbedAsync(null, Title: lang["avatar_user", "USER", User], ImageUrl: User.GetAvatarUrl(size: 256));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Avatar", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Avatar", null, ex), Context);
            }
        }

        [Command("userid")]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Optional, Remainder]string User)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var user = await Utilities.GetUser(Context.Guild, User);
                if (user == null)
                    await Context.Channel.SendEmbedAsync(lang["util_user_not_found"]);
                else
                    await Context.Channel.SendEmbedAsync(lang["util_userid", "USER", user.Username, "ID", user.Id]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "UserId", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "UserId", null, ex), Context);
            }
        }

        [Command("remindme")]
        public async Task SetReminder([Optional, Remainder]string Arg)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);

                string[] Data = null;
                DateTime time = new DateTime();

                if (Arg == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["remindme_bad_time"]);
                    return;
                }

                if (Arg.Contains("@")) //Date time
                {
                    Data = Arg.Split("@");
                    string Time = Data[Data.Length - 1];
                    if (!DateTime.TryParse(Time, out time) && !Arg.Contains(" in "))
                    {
                        await Context.Channel.SendEmbedAsync(lang["remindme_bad_time"]);
                        return;
                    }
                    goto Valid;
                }
                if (Arg.Contains(" in ")) //Time span
                {
                    Data = Arg.Split(" in ");
                    string Time = Data[Data.Length - 1];

                    //TODO: Allow different formats: 1h 2m, 1 hour, 2 min, etc

                    if (!TimeSpan.TryParse(Time, out TimeSpan In))
                    {
                        await Context.Channel.SendEmbedAsync(lang["remindme_bad_time"]);
                        return;
                    }
                    time = DateTime.Now.Add(In);
                }
                else
                {
                    await Context.Channel.SendEmbedAsync(lang["remindme_invalid"]);
                    return;
                }
            Valid:
                string Text = string.Empty;
                for (int i = 0; i < Data.Length - 1; i++)
                    Text += Data[i];

                DateTime UTC = time.ToUniversalTime();
                string _Time = UTC.ToString("HHmmss");
                string _Date = UTC.ToString("yyyyMMdd");

                //TODO: Support for more time zones
                int n = 3902;
                var zone = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                if (zone.Minutes == 0)
                {
                    if (zone.Hours < 8 && zone.Hours > 1)
                        n += zone.Hours;
                }

                string TimeZoneConverter = $"https://www.timeanddate.com/worldclock/converter.html?iso={_Date}T{_Time}&p1={n}";
                var data = new List<object>()
                {
                    lang,
                    Text
                };
                EventScheduler.Current.Add(new Event(new ulong[1] { Context.User.Id }, EventType.Reminder, 1, time, new TimeSpan(), data, false, "Reminder", true, false));
                await Context.Channel.SendEmbedAsync(lang["remindme_success", "TIME", time.ToString(), "URL", TimeZoneConverter, "OFFSET", n - 3902]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "RemindMe", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RemindMe", null, ex), Context);
            }
        }
        public static async void SendReminder(Event obj)
        {
            if (Program.Bot.DiscordClient.LoginState != LoginState.LoggedIn)
            {
                obj.Run = obj.Run.Add(new TimeSpan(0, 1, 0));
                obj.Remaining = 1;
                EventScheduler.Current.Add(obj);
            }

            var User = Program.Bot.DiscordClient.GetUser(obj.Ids[0]);
            if (User == null)
                return;

            var l = obj.Data as List<object>;
            var lang = l[0] as LanguageDictionary;
            await User.SendEmbedAsync(lang["remindme_remindtext", "TEXT", l[1], "TIME", obj.Run.ToString()]);
        }

        [Command("afkmessage")]
        public async Task SetAfkMessage([Optional, Remainder]string Message)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var User = GuildDatabase.GetOrCreate(Context.Guild.Id, Context.User.Id);
                if (Message == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["afkmessage_empty"]);
                    return;
                }
                else if (Message.ToLower() == "remove")
                {
                    if (User.AfkMessage == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["afkmessage_not_set"]);
                        return;
                    }
                    User.AfkMessage = null;
                    await Context.Channel.SendEmbedAsync(lang["afkmessage_removed"]);
                }
                else
                {
                    if (User.AfkMessage != null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["afkmessage_already_set"]);
                        return;
                    }
                    User.AfkMessage = Message.Unmention();
                    await Context.Channel.SendEmbedAsync(lang["afkmessage_set"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "AfkMessage", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "AfkMessage", null, ex), Context);
            }
        }
        public static async Task SendAfkMessage(ICommandContext Context)
        {
            List<ulong> Sent = new List<ulong>();
            foreach (ulong User in Context.Message.MentionedUserIds)
            {
                if (Sent.Contains(User))
                    continue;
                var GuildUser = GuildDatabase.GetOrFake(Context.Guild.Id, User);
                if (GuildUser.AfkMessage != null)
                {
                    var user = await Context.Guild.GetUserAsync(GuildUser.UserId);
                    if (user != null)
                        await Context.Channel.SendEmbedAsync('"' + GuildUser.AfkMessage + $"{'"'} - **{user.Username}**");
                }
                Sent.Add(User);
            }
        }
    }
}