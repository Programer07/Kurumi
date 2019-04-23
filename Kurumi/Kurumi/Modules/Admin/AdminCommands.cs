using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services;
using Kurumi.Services.Permission;
using Kurumi.Services.Random;
using Kurumi.StartUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Admin
{
    public class AdminCommands : ModuleBase
    {
        public static List<string> DisabledCommands = new List<string>();

        [Command("command")]
        public async Task CommandManager([Optional] string command)
        {
            if (!Config.Administrators.Contains(Context.User.Id))
                return;

            command = command?.ToLower();

            switch(command)
            {
                case "list":
                    if (DisabledCommands.Count == 0)
                        await Context.Channel.SendEmbedAsync("No disabled commands found.");
                    else
                        await Context.Channel.SendEmbedAsync(string.Join($"\n", DisabledCommands), "Disabled commands:");
                    break;
                case "allcommand":
                    DisabledCommands.Clear();
                    await Context.Channel.SendEmbedAsync(":white_check_mark: | Successfuly enabled all commands.");
                    break;
                case "command":
                    await Context.Channel.SendEmbedAsync("No.");
                    break;
                default:
                    if(!PermissionManager.CommandExists(command))
                    {
                        await Context.Channel.SendEmbedAsync(":exclamation: Command not found!");
                        return;
                    }
                    bool Enable = DisabledCommands.Contains(command);
                    var aliases = PermissionManager.GetAliases(command);
                    foreach (var item in aliases)
                    {
                        if (Enable)
                            DisabledCommands.Remove(item);
                        else
                            DisabledCommands.Add(item);
                    }
                    if (Enable)
                        await Context.Channel.SendEmbedAsync($":white_check_mark: | Successfuly enabled ``{command}``!");
                    else
                        await Context.Channel.SendEmbedAsync($":white_check_mark: | Successfuly disabled ``{command}``!");
                    break;
            }
            await Utilities.Log(new LogMessage(LogSeverity.Info, "Command Manager", "success"), Context);
        }

        [Command("sudo")]
        public async Task Sudo([Optional, Remainder]string input)
        {
            if (!Config.Administrators.Contains(Context.User.Id))
                return;
            await Context.Channel.SendMessageAsync(input);
        }

        [Command("randomstats")]
        public async Task GetRandomStats()
        {
            try
            {
                if (KurumiRandom.LastRequest.Day != DateTime.Now.Day)
                {
                    KurumiRandom rand = new KurumiRandom();
                    rand.Next(0, 10);
                }
                await Context.Channel.SendEmbedAsync($"Requests left: {KurumiRandom.RequestsLeft}\nBits left: {KurumiRandom.BitsLeft}");
                await Utilities.Log(new LogMessage(LogSeverity.Info, "RandomStats", "success"), null);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RandomStats", null, ex), Context);
            }
        }

        [Command("ssl")]
        public async Task SSLMode([Optional]bool? State)
        {
            try
            {
                if (!Config.Administrators.Contains(Context.User.Id))
                    return;
                if (State == null)
                {
                    await Context.Channel.SendEmbedAsync($"SSL Mode: ``{Config.SSLEnabled}``");
                }
                else
                {
                    Config.SSLEnabled = State.Value;
                    await Context.Channel.SendEmbedAsync($"SSL mode set to: ``{Config.SSLEnabled}``");
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "SSL", "success"), Context);
            }
            catch (Exception ex) //Can't send?
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "SSL", null, ex), Context);
            }
        }

        [Command("event")]
        public async Task Events([Optional, Remainder]string id)
        {
            try
            {
                if (!Config.Administrators.Contains(Context.User.Id))
                    return;

                if (id == null)
                    await Context.Channel.SendMessageAsync(EventScheduler.Current.ToString());
                else
                {
                    var t = EventScheduler.Current.Events.FirstOrDefault(x => x.Id.ToString() == id.Substring(0, 3));
                    if (t != null)
                    {
                        if (id.EndsWith(" --exception"))
                            await Context.Channel.SendEmbedAsync($"**Exception view: Event #{id.Remove(" --exception")}**:\n``{t.Error?.ToString() ?? "Empty"}``");
                        else if (id.EndsWith(" --markread"))
                            t.Error = null;
                        else
                            await Context.Channel.SendEmbedAsync($"**Name:** {t.Name}\n" +
                                                                 $"**Status:** {t.State}\n" +
                                                                 $"**Type:** {t.Type}\n" +
                                                                 $"**Fail:** {t.Error != null}\n" +
                                                                 $"**Run times remaining:** {t.Remaining}\n" +
                                                                 $"**Run at:** {t.Run}\n" +
                                                                 $"**Run again after:** {t.Interval.TotalSeconds}s\n" +
                                                                 $"**Allow parallel:** {t.AllowParallel}\n" +
                                                                 $"**Ignore fail:** {t.IgnoreFail}\n" +
                                                                 $"**Has info attached:** {t.Data != null}");
                    }
                    else
                        await Context.Channel.SendEmbedAsync(":exclamation: Event not found!");
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Events", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Events", null, ex), Context);
            }
        }

        [Command("statusmessage")]
        public async Task SetPlayingStatus([Remainder, Optional]string Message)
        {
            try
            {
                if (!Config.Administrators.Contains(Context.User.Id))
                    return;

                if (Message == null)
                {
                    string Messages = string.Empty;
                    for (int i = 0; i < Program.Bot.StatusMessages.Count; i++)
                    {
                        var status = Program.Bot.StatusMessages[i];
                        Messages += $"{i + 1}) {ConvertActivity(status.type)}{status.msg}\n";
                    }
                    await Context.Channel.SendEmbedAsync(Messages);
                }
                else if (Message.StartsWith("add "))
                {
                    string msg = Message.Remove("add ");
                    ActivityType activity = ActivityType.Playing;
                    if (msg.Contains(" ||| "))
                    {
                        if (!Enum.TryParse(msg.Split("|||")[1], out activity))
                            activity = ActivityType.Playing;
                        else
                            msg = msg.Split("|||")[0];
                    }
                    Program.Bot.StatusMessages.Add((msg, activity));
                    await Context.Channel.SendEmbedAsync($":white_check_mark: | Successfuly added: '{ConvertActivity(activity)}{msg}'");

                }
                else if (Message.StartsWith("remove "))
                {
                    string id = Message.Remove("remove ");
                    if (int.TryParse(id, out int i) && i > 0 && i <= Program.Bot.StatusMessages.Count)
                    {
                        Program.Bot.StatusMessages.RemoveAt(i - 1);
                        await Context.Channel.SendEmbedAsync($":white_check_mark: | Successfuly removed!");
                    }
                    else
                    {
                        await Context.Channel.SendEmbedAsync(":exclamation: Invalid Id!");
                    }
                }
                else
                {
                    await Context.Channel.SendEmbedAsync(":exclamation: Bad syntax!");
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "StatusMessage", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "StatusMessage", null, ex), Context);
            }
        }

        private string ConvertActivity(ActivityType s)
        {
            switch (s)
            {
                case ActivityType.Playing:
                case ActivityType.Streaming:
                case ActivityType.Watching:
                    return s.ToString() + " ";
                case ActivityType.Listening:
                    return "Listening to ";
            }
            return string.Empty;
        }
    }
}