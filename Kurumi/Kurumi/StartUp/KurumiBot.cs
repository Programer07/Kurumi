using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Modules;
using Kurumi.Services;
using Kurumi.Services.Random;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Kurumi.StartUp
{
    public class KurumiBot
    {
        public DiscordShardedClient DiscordClient;
        public IServiceProvider Services;
        public CommandHandler CommandHandler;
        public StartupState State;
        private int StatusIndex;
        private Random rng;
        private bool Freeze;

        public async Task Start()
        {
            State = StartupState.DiscordInitializing;
            int RetryCount = 0;
            try
            {
                //Create discord client
                DiscordClient = new DiscordShardedClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 50,
                    AlwaysDownloadUsers = true,
                    TotalShards = Config.ShardCount
                });

                //Setup events
                DiscordClient.Log += (LogMessage lmsg) => Utilities.Log(lmsg, null);
                DiscordClient.UserJoined += WelcomeMessage.SendMessage;
                if (Config.BotlistApiKey != null) //Check if enabled
                {
                    DiscordClient.JoinedGuild += (_) => DiscordBotlist.UpdateServerCount();
                    DiscordClient.LeftGuild += (_) => DiscordBotlist.UpdateServerCount();
                }

                //Setup command handler
                CommandHandler = new CommandHandler(this);

                //Setup services
                Services = new ServiceCollection()
                   .AddSingleton(DiscordClient)
                   .AddSingleton(CommandHandler.Commands)
                   .BuildServiceProvider();
            }
            catch (Exception)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Critical, "Start", "Failed to create discord client / register commands & events!"));
                Console.ReadLine();
                Environment.Exit(0);
            }
        //Connect to discord
        Reconnect:
            State = StartupState.DiscordLogin;
            try
            {
                //Login
                await DiscordClient.LoginAsync(TokenType.Bot, Config.BotToken);
                await DiscordClient.StartAsync();
                await DiscordClient.SetStatusAsync(UserStatus.Online);
                State = StartupState.Ready;
                //Set it to 0 to reset reconnecting
                RetryCount = 0;
                while(true)
                {
                    while (State != StartupState.Ready) await Task.Delay(1000);
                    if (Console.ReadLine().ToLower() == "quit")
                    {
                        await Program.Quit();
                        Program.ExitHandled = true;
                        Environment.Exit(0);
                    }
                }
            }
            catch (Exception) //Internet disconnect?
            {
                if (RetryCount != 3)
                    RetryCount++;
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Main", $"Failed to connect... Retring in {10 * RetryCount} seconds"));
                await Task.Delay(1000 * 10 * RetryCount);

                //Reset discord client
                try
                {
                    await DiscordClient.LogoutAsync();
                }
                catch (Exception) { }
                try
                {
                    await DiscordClient.StopAsync();
                }
                catch (Exception) { }

                goto Reconnect;
            }
        }
        public async Task Shutdown()
        {
            await DiscordClient.LogoutAsync();
            while (DiscordClient.LoginState != LoginState.LoggedOut) await Task.Delay(10);
            await DiscordClient.StopAsync();
            DiscordClient.Dispose();
        }


        public List<(string msg, ActivityType type)> StatusMessages = new List<(string, ActivityType)>
        {
            ( "with @GUILDS@ server's life | !k.help", ActivityType.Playing ),
            ( "!k.help for help", ActivityType.Playing ),
            ( "with @OWNER@ | !k.help", ActivityType.Playing ),
            ( "support me on Patreon | !k.donate", ActivityType.Playing ),
            ( "challenge me! | !k.duel @Kurumi#3030", ActivityType.Playing ),
            ( "you | !k.help", ActivityType.Watching ),
            ( "Vote for Kurumi on discordbots.org | !k.vote", ActivityType.Playing ),
        };
        public void NextPlayingStatus()
        {
            Task.Run(async () => 
            {
                while (DiscordClient == null || DiscordClient.LoginState != LoginState.LoggedIn) { }

                if (rng == null)
                    rng = new Random();

                Get:
                if (!Freeze)
                {
                    var i = rng.Next(0, StatusMessages.Count);
                    if (i == StatusIndex)
                        goto Get;
                    StatusIndex = i;
                }
                var (msg, type) = StatusMessages[StatusIndex];
                var application = await DiscordClient.GetApplicationInfoAsync();
                await DiscordClient.SetGameAsync(msg.Replace("@GUILDS@", $"{DiscordClient.Guilds.Count}").Replace("@OWNER@", $"{application.Owner}"), null, type);
                return Task.CompletedTask;
            });
        }
        public bool FreezeStatusMessage() => Freeze = !Freeze;
    }

    public enum StartupState : byte
    {
        Loading,
        DatabasesLoading,
        DatabaseReady,
        EventsLoading,
        EventsLoaded,
        DiscordInitializing,
        DiscordLogin,
        Ready
    }
}