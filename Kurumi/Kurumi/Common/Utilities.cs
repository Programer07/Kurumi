using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Random;
using Kurumi.StartUp;
using Sentry;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Common
{
    public class Utilities
    {
        private static SentryClient _SentryClient;
        private const string SUCCESS = "Command successfuly finished";
        public static object WriteLock { get; set; } = new object();
        public static Task Log(LogMessage lmsg, ICommandContext Context = null)
        {
            lock (WriteLock)
            {
                try
                {
                    if (lmsg.Severity != LogSeverity.Error && lmsg.Message != null)
                        for (int i = 0; i < IgnoreLogger.Length; i++)
                            if (lmsg.Message.Contains(IgnoreLogger[i]))
                                return Task.CompletedTask;

                    if (lmsg.Exception != null && lmsg.Exception is HttpException httpEx)
                    {
                        if (httpEx.DiscordCode == 500)
                        {
                            Log(new LogMessage(LogSeverity.Warning, "Discord", "(500) Internal server error!")/*, Context*/);
                            return Task.CompletedTask;
                        }
                        else if (httpEx.DiscordCode == 50013)
                        {
                            Log(new LogMessage(LogSeverity.Warning, "Discord", "(50013) Missing permission!"), Context);
                            return Task.CompletedTask;
                        }
                    }

                    Console.ResetColor();
                    ConsoleColor color = ConsoleColor.Black;
                    switch (lmsg.Severity)
                    {
                        case LogSeverity.Critical:
                        case LogSeverity.Error:
                            color = ConsoleColor.Red;
                            if (lmsg.Exception != null)
                            {
                                LogSentry(lmsg, Context);
                                DMErrorMessage(lmsg.Exception);
                                if (Context != null)
                                    ErrorMessage(Context, lmsg.Exception);
                            }
                            break;
                        case LogSeverity.Warning:
                            color = ConsoleColor.Yellow;
                            break;
                        case LogSeverity.Info:
                            color = ConsoleColor.Green;
                            break;
                        case LogSeverity.Verbose:
                        case LogSeverity.Debug:
                            color = ConsoleColor.Green;
                            break;
                    }

                    Console.Write($"[{DateTime.Now} |");
                    Console.ForegroundColor = color;
                    Console.Write($"{FormatSeverity(lmsg.Severity),6}");
                    Console.ResetColor();
                    Console.Write($"] {lmsg.Source}: {GetMessage(lmsg, Context)}\n");
                }
                catch (Exception ex)
                {
                    Console.ResetColor();
                    Console.WriteLine($"Logger error! Severity: {lmsg.Severity} | " +
                                                    $"Source: {lmsg.Source} | " +
                                                    $"Message: {lmsg.Message ?? "-"} | " +
                                                    $"Exception: {lmsg.Exception?.ToString() ?? "-"}\n" +
                                                    $"Logger exception: {ex}");
                }
                return Task.CompletedTask;
            }
        }
        private static Task LogSentry(LogMessage lmsg, ICommandContext context)
        {
            return Task.Run(() => 
            {
                if (Config.SentryApiKey == null || Config.Environment == Config.KurumiEnvironment.Development)
                    return Task.CompletedTask;

                if (_SentryClient == null)
                    _SentryClient = new SentryClient(new SentryOptions()
                    {
                        Release = KurumiData.Get().Version,
                        Dsn = new Dsn(Config.SentryApiKey),
                        AttachStacktrace = true
                    });


                SentryEvent Event;
                if (lmsg.Exception == null)
                {
                    Event = new SentryEvent()
                    {
                        Level = GetLevel(lmsg.Severity),
                        Message = lmsg.Message,
                        User = new User() { Username = context == null ? "System/Unknown" : context.User.Username },
                    };
                }
                else
                {
                    Event = new SentryEvent(lmsg.Exception)
                    {
                        Level = GetLevel(lmsg.Severity),
                        Message = lmsg.Message,
                        User = new User() { Username = context == null ? "System/Unknown" : context.User.Username }
                    };
                }
                Event.SetTags(new Dictionary<string, string>() { { "Source", lmsg.Source } });
                _SentryClient.CaptureEvent(Event);
                return Task.CompletedTask;
            });
        }
        private static Task DMErrorMessage(Exception ex)
        {
            return Task.Run(async () =>
            {
                if (Config.Administrators.Length == 0 || Config.Environment == Config.KurumiEnvironment.Development)
                    return Task.CompletedTask;

                var User = Program.Bot.DiscordClient.GetUser(Config.Administrators[0]);
                if (User == null)
                    return Task.CompletedTask;

                await User.SendEmbedAsync($"An error occurred!\n```{ex.ToString().Replace("`", "'")}```");
                return Task.CompletedTask;
            });
        }
        private static Task ErrorMessage(ICommandContext Context, Exception ex)
        {
            return Task.Run(async () => 
            {
                try
                {
                    KurumiRandom rand = new KurumiRandom();
                    string[] Messages =
                    {
                        "An error occurred.",
                        "Something went wrong while executing the command.",
                        "Something isn't working."
                    };
                    string Message;
                    if (rand.Next(0, 50) == 0)
                    {
                        string[] SpecialMessages =
                        {
                            "I blame Microsoft for this error!",
                            "My head hurts!"
                        };
                        Message = SpecialMessages[rand.Next(0, SpecialMessages.Length)];
                    }
                    else
                        Message = Messages[rand.Next(0, Messages.Length)];
                    Message += $"\n:warning: **{ex.GetType()}:** {ex.Message.Replace(KurumiPathConfig.Root, "KURUMI_ROOT" + KurumiPathConfig.Separator)}";
                    await Context.Channel.SendEmbedAsync(Message, "Error");
                }
                catch (Exception)
                { }
            });
        }
        private static string FormatSeverity(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    return "Crit";
                case LogSeverity.Warning:
                    return "Warn";

                case LogSeverity.Error:
                case LogSeverity.Info:
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    return severity.ToString();
            }
            return "Oof";
        }
        private static string GetMessage(LogMessage lmsg, ICommandContext Context)
        {
            if (Context == null || Config.LoggerMode == 0)
            {
                if (lmsg.Severity == LogSeverity.Info)
                {
                    if (lmsg.Message == "success")
                        return SUCCESS;
                    return lmsg.Message;
                }
                else
                {
                    return $"{(lmsg.Message == null ? lmsg.Exception.ToString() : lmsg.Message + " " + lmsg.Exception)}";
                }
            }
            else
            {
                string Message = string.Empty;
                if (lmsg.Severity == LogSeverity.Info && lmsg.Message == "success")
                    Message += $"{SUCCESS}";
                else
                    Message += $"{(lmsg.Message == null ? lmsg.Exception?.ToString() : lmsg.Message + lmsg.Exception)}";

                if(Config.LoggerMode < 4) //Id
                {
                    if (Config.LoggerMode > 0)
                        Message += $", guild: {(Context.Guild == null ? "(DM)" : $"{Context.Guild.Id}")}";
                    if (Config.LoggerMode > 2 && Context.Guild != null)
                        Message += $", channel: {Context.Channel.Id}";
                    if (Config.LoggerMode > 1)
                        Message += $", user: {Context.User.Id}";
                }
                else if (Config.LoggerMode < 7) //Name
                {
                    if (Config.LoggerMode > 3)
                        Message += $", guild: {(Context.Guild == null ? "(DM)" : $"{Context.Guild.Name}")}";
                    if (Config.LoggerMode > 5 && Context.Guild != null)
                        Message += $", channel: {Context.Channel.Name}";
                    if (Config.LoggerMode > 4)
                        Message += $", user: {Context.User.Username}";
                }
                else if (Config.LoggerMode < 10) //Small id
                {
                    if (Config.LoggerMode > 6)
                        Message += $", g: {(Context.Guild == null ? "(DM)" : $"{Context.Guild.Id}")}";
                    if (Config.LoggerMode > 8 && Context.Guild != null)
                        Message += $", c: {Context.Channel.Id}";
                    if (Config.LoggerMode > 7)
                        Message += $", u: {Context.User.Id}";
                }
                else if (Config.LoggerMode < 13) //Small name
                {
                    if (Config.LoggerMode > 9)
                        Message += $", g: {(Context.Guild == null ? "(DM)" : $"{Context.Guild.Name}")}";
                    if (Config.LoggerMode > 11 && Context.Guild != null)
                        Message += $", c: {Context.Channel.Name}";
                    if (Config.LoggerMode > 10)
                        Message += $", u: {Context.User.Username}";
                }

                return Message;
            }
        }
        private static SentryLevel GetLevel(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    return SentryLevel.Fatal;
                case LogSeverity.Error:
                    return SentryLevel.Error;
                case LogSeverity.Warning:
                    return SentryLevel.Warning;
                case LogSeverity.Info:
                    return SentryLevel.Info;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    return SentryLevel.Debug;
                default:
                    return SentryLevel.Error;
            }
        }

        public static async Task<IUser> GetUser(IGuild guild, string Input)
        {
            if (Input == null)
                return null;

            var GuildUsers = await guild.GetUsersAsync();
            foreach (var User in GuildUsers)
            {
                if(User.Username.Equals(Input, StringComparison.CurrentCultureIgnoreCase) || 
                   User.Id.ToString() == Input || 
                   User.ToString().Equals(Input, StringComparison.CurrentCultureIgnoreCase) ||
                   User.Mention.Remove("!").Equals(Input.Remove("!"), StringComparison.CurrentCultureIgnoreCase))
                {
                    return User;
                }
            }
            return null;
        }
        public static async Task<IGuildChannel> GetChannel(IGuild guild, string Input)
        {
            if (Input == null)
                return null;

            var GuildChannels = await guild.GetChannelsAsync();
            foreach (var Channel in GuildChannels)
            {
                if (Channel.Name.Equals(Input, StringComparison.CurrentCultureIgnoreCase) ||
                   Channel.Id.ToString() == Input ||
                   Channel.ToString().Equals(Input, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Channel;
                }
                else if (Channel is ITextChannel textChannel && textChannel.Mention == Input)
                {
                    return Channel;
                }
            }
            return null;
        }
        public static IRole GetRole(IGuild guild, string Input)
        {
            if (Input == null)
                return null;

            var GuildRoles = (guild as SocketGuild).Roles;
            foreach (var Role in GuildRoles)
            {
                if (Role.Name.Equals(Input, StringComparison.CurrentCultureIgnoreCase) ||
                   Role.Id.ToString() == Input ||
                   Role.ToString().Equals(Input, StringComparison.CurrentCultureIgnoreCase) ||
                   Role.Mention.Remove("!").Equals(Input.Remove("!")))
                {
                    return Role;
                }
            }
            return null;
        }

        private static readonly string[] IgnoreLogger =
        {
            "Connecting",
            "Disconnecting",
            "Unknown OpCode (8)",
            "Unknown OpCode (13)",
            "Unknown OpCode (12)",
            "Unknown OpCode (Hello)",
            "Unknown User"
        };
    }
}