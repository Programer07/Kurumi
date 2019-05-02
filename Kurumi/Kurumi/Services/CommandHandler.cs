using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Attributes;
using Kurumi.Common.Extensions;
using Kurumi.Modules.Admin;
using Kurumi.Modules.Games.LobbyGames;
using Kurumi.Modules.Games.LobbyGames.Chess;
using Kurumi.Modules.Games.LobbyGames.Duel;
using Kurumi.Modules.Games.LobbyGames.Quiz;
using Kurumi.Modules.LobbyGames.Games;
using Kurumi.Modules.Moderation;
using Kurumi.Modules.Music;
using Kurumi.Modules.Utility;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Leveling;
using Kurumi.Services.Permission;
using Kurumi.StartUp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kurumi.Services
{
    public class CommandHandler
    {
        public const string DEFAULT_PREFIX = "!k.";
        private const int MILLISECONDS = 2000;
        private readonly ConcurrentDictionary<(Type, string), bool> AttributeCache = new ConcurrentDictionary<(Type, string), bool>();
        private readonly ConcurrentDictionary<ulong, ChannelLimit> Limit = new ConcurrentDictionary<ulong, ChannelLimit>();
        private readonly ConcurrentList<ulong> BlockedChannels = new ConcurrentList<ulong>();
        public CommandService Commands;
        public KurumiBot Bot;

        public CommandHandler(KurumiBot bot)
        {
            Bot = bot;
            Commands = new CommandService(new CommandServiceConfig { DefaultRunMode = RunMode.Async });
            Bot.DiscordClient.MessageReceived += (msg) => Task.Run(async () => { await MessageReceivedHandler(msg); return Task.CompletedTask; });
            var t = Task.Run(async () => { await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), Bot.Services); return Task.CompletedTask; });
            while (t.IsCompleted)
                Thread.Sleep(10);
        }


        public async Task MessageReceivedHandler(SocketMessage messageReceived)
        {
            if (!(messageReceived is SocketUserMessage msg))
                return;
            var Context = new CommandContext(Bot.DiscordClient, msg);
            if (BlockedChannels.Contains(Context.Channel.Id))
                return;

            if (Context.User.IsBot && Config.Environment != Config.KurumiEnvironment.Development) //Ignore bot commands
                return;

            //Get prefix
            string prefix = DEFAULT_PREFIX;
            if (Context.Channel.GetType().ToString() == "Discord.WebSocket.SocketTextChannel") //There has to be a better way to check this
            {
                prefix = GuildDatabase.GetOrFake(Context.Guild.Id).Prefix;
                await ExpManager.AddExp(Context); //Only give EXP on servers
            }

            if (Config.Environment == Config.KurumiEnvironment.Development)
                prefix = "k.";

            //Check prefix
            int argPos = 0;
            if (!(msg.HasStringPrefix(prefix, ref argPos) || msg.HasMentionPrefix(Bot.DiscordClient.CurrentUser, ref argPos)))
            {
                await Prefixless(Context);
                return;
            }

            //Get the command string
            string content = msg.Content.Substring(argPos);
            int i = content.Length;
            if (content.Contains(' '))
                i = content.IndexOf(' ');
            string command = content.Substring(0, i).ToLower();

            //Check if the command exists
            if (!PermissionManager.CommandExists(command))
                return;

            //Check if the command is disabled
            var lang = Language.GetLanguage(Context.Guild);
            if(AdminCommands.DisabledCommands.Contains(command))
            {
                await Context.Channel.SendEmbedAsync(lang["commandhandler_command_disabled"]);
                return;
            }

            //Check if the user has permission
            if(!PermissionManager.HasPermission(Context, command))
            {
                await Context.Channel.SendEmbedAsync(lang["commandhandler_no_permission", "COMMAND", command]);
                return;
            }

            if (Limit.ContainsKey(Context.Channel.Id))
            {
                var c = Limit[Context.Channel.Id];
                if (DateTime.Now.Subtract(c.Added).TotalMilliseconds > MILLISECONDS)
                {
                    c.Count = 0;
                    c.Added = DateTime.Now;
                }
                else
                {
                    c.Count++;
                    if (c.Count == 3)
                    {
                        BlockedChannels.Add(Context.Channel.Id);
                        await Context.Channel.SendEmbedAsync(lang["ratelimit_slow_down"]);
                        await RemoveChannelAfter(Context.Channel.Id, MILLISECONDS);
                        await Utilities.Log(new LogMessage(LogSeverity.Warning, "RateLimit", "Channel blocked"), Context);
                        return;
                    }
                }
            }
            else
                Limit.TryAdd(Context.Channel.Id, new ChannelLimit() { Count = 1, Added = DateTime.Now });
            //Check custom attributes
            if (Config.BotlistApiKey != null)
            {
                if (HasCustomAttribute(typeof(RequireUserVoteAttribute), command) && !await DiscordBotlist.UserVoted(Context.User.Id))
                {
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_need_vote"]);
                    return;
                }

                if (HasCustomAttribute(typeof(RequireGuildOwnerVoteAttribute), command) && !await DiscordBotlist.UserVoted(Context.Guild.OwnerId))
                {
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_owner_need_vote", "OWNER", (await Context.Guild.GetOwnerAsync()).Id]);
                    return;
                }
            }

            //Execute
            IResult result = await Commands.ExecuteAsync(Context, argPos, Program.Bot.Services);
            if (!result.IsSuccess)
            {
                if (result.ErrorReason == "Unknown command.")
                    return;
                else if (result.ErrorReason.Contains("Bot requires channel permission") || result.ErrorReason.Contains("Bot requires guild permission"))
                {
                    if (result.ErrorReason.Contains("Embed"))
                        await Context.Channel.SendMessageAsync(lang["commandhandler_no_bot_permission", "PERMISSION", $"{result.ErrorReason.Remove("Bot requires channel permission ").Remove("Bot requires guild permission")}"]);
                    else
                        await Context.Channel.SendEmbedAsync(lang["commandhandler_no_bot_permission", "PERMISSION", $"{result.ErrorReason.Remove("Bot requires channel permission ").Remove("Bot requires guild permission")}"]);
                }
                else if (result.ErrorReason.Contains("The input text has too many parameters."))
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_too_many_parameters"]);
                else if (result.ErrorReason.Contains("The input text has too few parameters."))
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_too_few_parameters"]);
                else if (result.ErrorReason.Contains("User requires guild permission") || result.ErrorReason.Contains("User requires channel permission"))
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_user_no_permission", "PERMISSION", $"{result.ErrorReason.Remove("User requires guild permission").Remove("User requires channel permission")}"]);
                else if (result.ErrorReason.Contains("User not found"))
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_user_not_found"]);
                else if (result.ErrorReason.Contains("Failed to parse"))
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_failed_parse"]);
                else if (result.ErrorReason == "Invalid context for command; accepted contexts: Guild.")
                    await Context.Channel.SendEmbedAsync(lang["commandhandler_context_error"]);
                else if (msg.HasMentionPrefix(Bot.DiscordClient.CurrentUser, ref argPos))
                    await Context.Channel.SendEmbedAsync("Unhandled command error: " + result.ErrorReason);
            }
        }
        private static async Task Prefixless(CommandContext context)
        {
            if (context.Guild == null)
                return; //If the command was executed from DM the guild will be null

            #region Blacklist
            await Warnings.CheckForWord(context);
            #endregion Blacklist

            #region Music
            if (Music.Selecting.ContainsKey(context.Guild.Id))
            {
                var s = Music.Selecting[context.Guild.Id];
                if (s.User == context.User.Id)
                {
                    if (context.Message.Content.Equals("cancel", StringComparison.CurrentCultureIgnoreCase))
                    {
                        s.Selected = -2;
                        return;
                    }
                    else if (int.TryParse(context.Message.Content, out int Pick) && Pick <= 10 && Pick > 0)
                    {
                        s.Selected = Pick;
                        return;
                    }
                }
            }
            #endregion Music

            #region Games
            for (int i = 0; i < LobbyManager.Lobbies.Count; i++)
            {
                var lobby = LobbyManager.Lobbies[i];
                if(lobby.Context.Guild.Id == context.Guild.Id && !(lobby.Game is NoneGame))
                {
                    if (lobby.Game is ChessGame chess && chess.Player == context.User.Id)
                    {
                        if (chess.Move == null && //Move, TODO: regex
                                context.Message.Content.Length == 5 &&
                                char.IsNumber(context.Message.Content[1]) &&
                                char.IsNumber(context.Message.Content[4]) &&
                                char.IsLetter(context.Message.Content[0]) &&
                                char.IsLetter(context.Message.Content[3]) &&
                                context.Message.Content[2] == ';')
                            chess.Move = context.Message.Content;
                        else if (chess.Promote == null && Enum.TryParse(context.Message.Content, true, out ChessGame.PromotePieces piece)) //Promote
                            chess.Promote = piece;
                    }
                    else if (lobby.Game is QuizGame quiz && quiz.Answer == null && quiz.player.Id == context.User.Id && Enum.TryParse(context.Message.Content, true, out QuizGame.QuizAnswer Answer))
                    {
                        quiz.Answer = Answer;
                    }
                    else if (lobby.Game is DuelGame duel && duel.Selected == null && duel.SelectingUser == context.User.Id && context.Message.Content.Length == 1)
                    {
                        string s = context.Message.Content.ToLower();
                        if (s == "a" || s == "x" || s == "b" || s == "y")
                            duel.Selected = s;
                    }
                }
            }
            #endregion Games

            await UserCommands.SendAfkMessage(context);
        }
        private bool HasCustomAttribute(Type attribute, string command)
        {
            if (AttributeCache.ContainsKey((attribute, command)))
                return AttributeCache[(attribute, command)];

            command = command.ToLower();
            bool res = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(ModuleBase))))
                .SelectMany(x => x.GetMethods()
                .Where(c => c.GetCustomAttributes(attribute, true).Length > 0)
                .Where(y => (((IEnumerable<CommandAttribute>)y.GetCustomAttributes(typeof(CommandAttribute), true))
                .Where(z => z.Text.ToLower() == command).Count() > 0) ||
                (((IEnumerable<AliasAttribute>)y.GetCustomAttributes(typeof(AliasAttribute), true))
                .Where(z => z.Aliases.ToList().ConvertAll(i => i.ToLower()).Contains(command)).Count() > 0))).Count() > 0;
            AttributeCache.TryAdd((attribute, command), res);
            return res;
        }
        private Task RemoveChannelAfter(ulong Channel, int Milliseconds = 2000)
        {
            Task.Run(async () => 
            {
                await Task.Delay(Milliseconds);
                BlockedChannels.Remove(Channel);
            });
            return Task.CompletedTask;
        }
        private class ChannelLimit
        {
            public DateTime Added { get; set; }
            public int Count { get; set; }
        }
    }

    public class TestCommand : ModuleBase
    {
        [Command("test")]
        [Alias("OwO")]
        public async Task Test()
        {
            //await Task.Delay(10 * 1000);
            //await ReplyAsync("<@&401122182534529024>");
            //throw new Exception("OwO What's this?");
            await Utilities.Log(new LogMessage(LogSeverity.Info, "Test", "success"), Context);
        }
    }
}