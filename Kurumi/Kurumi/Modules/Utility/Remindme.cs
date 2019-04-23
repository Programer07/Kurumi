using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services;
using Kurumi.StartUp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Utility
{
    public class Remindme : ModuleBase
    {
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


        public static async void Send(Event obj)
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
    }
}