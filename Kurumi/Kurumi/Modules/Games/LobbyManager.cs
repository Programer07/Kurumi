using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.StartUp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games
{
    public class LobbyManager : ModuleBase
    {
        public static ConcurrentList<Lobby> Lobbies = new ConcurrentList<Lobby>();
        public static ConcurrentList<LobbyInvite> Invites = new ConcurrentList<LobbyInvite>();

        public LanguageDictionary lang;

        [Command("lobby")]
        [RequireContext(ContextType.Guild)]
        public async Task LobbyCommand([Optional]string op, [Remainder, Optional]string arg1/*, [Optional]string arg2*/)
        {
            lang = Language.GetLanguage(Context.Guild);
            op = op?.ToLower();
            switch (op)
            {
                case "create": //lobby create [chess]
                    await Create(arg1);
                    break;
                case "invite": //lobby invite Noel-chan
                    await Invite(arg1);
                    break;
                case "delete": //lobby delete
                    await Delete();
                    break;
                case "kick": //lobby kick user
                    await Kick(arg1);
                    break;
                case "setgame":
                case "game": //lobby game uno
                    await SetGame(arg1);
                    break;
                case "leave": //lobby leave
                    await Leave();
                    break;
                case "start": //lobby start
                    await StartGame();
                    break;
                case "join": //lobby join
                case "accept": //lobby accept
                    await Accept();
                    break;
                case "decline":
                    await Decline();
                    break;
                case "settings": //lobby settings round-time=180
                    await Settings(arg1);
                    break;
                default:
                    var lobby = GetLobby();
                    if (lobby != null)
                    {
                        await Context.Channel.SendEmbedAsync(ToEmbedBuilder(lobby));
                    }
                    else
                    {
                        await Context.Channel.SendEmbedAsync(lang["lobby_help"]);
                    }
                    break;
            }
        }

        private async Task Create(string arg1)
        {
            try
            {
                if (IsPlayerInGame())
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_already_ingame"]);
                    return;
                }

                GameType gameType = GameType.None;
                if (arg1 != null && (!Enum.TryParse(arg1, true, out gameType) || gameType == GameType.None))
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_bad_game"]);
                    return;
                }

                //Create lobby
                var lobby = new Lobby(Context, lang["lobby_name", "USER", Context.User.Username], gameType);
                Lobbies.Add(lobby);

                //Send message
                await Context.Channel.SendEmbedAsync(ToEmbedBuilder(lobby));

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Create", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Create", null, ex), Context);
            }
        }
        private async Task Leave()
        {
            try
            {
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (lobby.Ingame && lobby.IndexOfPlayer(Context.User.Id) < lobby.Game.MaxPlayers)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_leave_ingame"]);
                    return;
                }
                lobby.RemovePlayer(Context.User.Id);
                await Context.Channel.SendEmbedAsync(lang["lobby_left", "USER", Context.User.Username]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Leave", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Leave", null, ex), Context);
            }
        }
        private async Task Delete()
        {
            try
            {
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if(lobby.Players[0].Id != Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_owner"]);
                    return;
                }
                else if(lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_ingame_delete"]);
                    return;
                }

                Lobbies.Remove(lobby);
                await Context.Channel.SendEmbedAsync(lang["lobby_deleted"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Delete", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Delete", null, ex), Context);
            }
        }
        private async Task Invite(string arg1)
        {
            try
            {
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (lobby.Players[0].Id != Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_owner"]);
                    return;
                }
                else if (lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_invite_ingame"]);
                    return;
                }

                IUser user = await Utilities.GetUser(Context.Guild, arg1);
                if(user == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_user_not_found"]);
                    return;
                }
                else if (GetInvite(user.Id, Context.Guild.Id) != null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_already_invited"]);
                    return;
                }
                else if (GetLobby(user.Id) != null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_user_already_ingame"]);
                    return;
                }


                if (user.Id == Program.Bot.DiscordClient.CurrentUser.Id)
                {
                    lobby.Players.Add(user);
                    await Context.Channel.SendEmbedAsync(lang["lobby_accepted", "LOBBY", lobby.Name]);
                }
                else
                {
                    var invite = new LobbyInvite()
                    {
                        Accepted = null,
                        Lobby = lobby,
                        User = user
                    };
                    Invites.Add(invite);
                    StartNewInviteTimer(invite);
                    await Context.Channel.SendEmbedAsync(lang["lobby_invited", "USER", user.Username, "LOBBY", lobby.Name]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Invite", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Invite", null, ex), Context);
            }
        }
        private async Task Accept()
        {
            try
            {
                if (IsPlayerInGame())
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_already_ingame"]);
                    return;
                }

                var Invite = GetInvite(Context.User.Id, Context.Guild.Id);
                if (Invite == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_invited"]);
                    return;
                }

                Invite.Accepted = true;
                await Context.Channel.SendEmbedAsync(lang["lobby_accepted", "LOBBY", Invite.Lobby.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Accept", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Accept", null, ex), Context);
            }
        }
        private async Task Decline()
        {
            try
            {
                if (IsPlayerInGame())
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_already_ingame"]);
                    return;
                }

                var Invite = GetInvite(Context.User.Id, Context.Guild.Id);
                if (Invite == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_invited"]);
                    return;
                }

                Invite.Accepted = false;
                await Context.Channel.SendEmbedAsync(lang["lobby_declined", "LOBBY", Invite.Lobby.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Decline", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Decline", null, ex), Context);
            }
        }
        private async Task Kick(string arg1)
        {
            try
            {
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (lobby.Players[0].Id != Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_owner"]);
                    return;
                }
                else if (lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_kick_ingame"]);
                    return;
                }

                IUser user = await Utilities.GetUser(Context.Guild, arg1);
                if (user == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_user_not_found"]);
                    return;
                }
                else if (user.Id == Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_invalid_kick"]);
                    return;
                }
                else if (!lobby.ContainsPlayer(user.Id))
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_user_not_in_lobby"]);
                    return;
                }

                lobby.RemovePlayer(user.Id);
                await Context.Channel.SendEmbedAsync(lang["lobby_kicked", "USER", user.Username, "LOBBY", lobby.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Kick", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Kick", null, ex), Context);
            }
        }
        private async Task SetGame(string arg1)
        {
            try
            {
                if (arg1 == null)
                {
                    await Context.Channel.SendEmbedAsync(GetGames(), lang["lobby_title"]);
                    return;
                }

                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (lobby.Players[0].Id != Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_owner"]);
                    return;
                }
                else if (lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_setting_ingame"]);
                    return;
                }

                GameType gameType = GameType.None;
                if (!Enum.TryParse(arg1, true, out gameType) || gameType == GameType.None)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_bad_game"]);
                    return;
                }

                lobby.SetGame(gameType);
                await Context.Channel.SendEmbedAsync(ToEmbedBuilder(lobby));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Set Game", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Set Game", null, ex), Context);
            }
        }
        private async Task StartGame()
        {
            try
            {
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (lobby.Players[0].Id != Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_owner"]);
                    return;
                }
                else if (lobby.SelectedGame == GameType.None)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_bad_game"]);
                    return;
                }
                else if (lobby.Game.MinPlayers > lobby.GetPlayers().Count)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_too_few_players"]);
                    return;
                }
                else if (lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_lobby_already_ingame"]);
                    return;
                }

                lobby.Start(Context);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Start", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Start", null, ex), Context);
            }
        }
        private async Task Settings(string arg1)
        {
            try
            {
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (lobby.Players[0].Id != Context.User.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_owner"]);
                    return;
                }
                else if (lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_setting_ingame"]);
                    return;
                }

                if (!arg1.Contains("="))
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_bad_setting"]);
                    return;
                }
                string[] Data = arg1.Split('=');
                if (string.IsNullOrEmpty(Data[0]) || string.IsNullOrWhiteSpace(Data[1]))
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_bad_setting"]);
                    return;
                }

                string Key = null;
                foreach (var setting in lobby.Game.Settings.Keys)
                {
                    if (setting.Equals(Data[0], StringComparison.CurrentCultureIgnoreCase))
                    {
                        Key = setting;
                        goto Found;
                    }
                }
                await Context.Channel.SendEmbedAsync(lang["lobby_setting_not_found"]);
                return;

            Found:
                try //Try converting user input to the type of the setting
                {
                    lobby.Game.Settings[Key] = Convert.ChangeType(Data[1], lobby.Game.Settings[Key].GetType());
                    await Context.Channel.SendEmbedAsync(lang["lobby_setting_success", "KEY", Key, "VAL", Data[1]]);
                }
                catch (Exception)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_setting_fail"]);
                }

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Lobby Settings", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Lobby Settings", null, ex), Context);
            }
        }


        [Command("surrender")]
        [RequireContext(ContextType.Guild)]
        public async Task Surrender()
        {
            try
            {
                lang = Language.GetLanguage(Context.Guild);
                var lobby = GetLobby();
                if (lobby == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_in_lobby"]);
                    return;
                }
                else if (!lobby.Ingame)
                {
                    await Context.Channel.SendEmbedAsync(lang["lobby_not_ingame"]);
                    return;
                }
                lobby.Game.Surrender.Add(Context.User.Id);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Surrender", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Surrender", null, ex), Context);
            }
        }

        /* (2019.04.11) This was written by my classmate:m.m
        private void test()
        {
            { await context.Channel.SendembedAsync(lang["lobby_notalready_ingame"]}
            
        }*/
        

        private void StartNewInviteTimer(LobbyInvite Invite)
        {
            Task.Run(async () => 
            {
                int Timer = 600;
                while(Timer != 0 && Invite.Accepted == null && !Invite.Lobby.Ingame)
                {
                    await Task.Delay(100);
                    Timer--;
                }

                if (Invite.Accepted == true)
                    Invite.Lobby.Players.Add(Invite.User);

                if (Invites.Contains(Invite))
                    Invites.Remove(Invite);
                return Task.CompletedTask;
            });
        }
        private EmbedBuilder ToEmbedBuilder(Lobby lobby)
        {
            return new EmbedBuilder()
                        .WithColor(Config.EmbedColor)
                        .WithTitle(lang["lobby_name", "USER", Context.User.Username])
                        .AddField(lang["lobby_top", "CURRENT", lobby.Players.Count, "MAX", lobby.Game.MaxPlayers], lobby.ToPlayerString(), true)

                        .AddField(lang["lobby_game"], $"{lang[lobby.Game.ToString()]}\n\n" +
                                                      $"{lang["lobby_max_players", "MAX", lobby.Game.MaxPlayers]}\n" +
                                                      $"{lang["lobby_min_players", "MIN", lobby.Game.MinPlayers]}\n\n" +
                                                      $"{lang["lobby_game_settings"]}\n" +
                                                      $"{ToSettingsString(lobby.Game)}", true)
                        .WithFooter(lang["lobby_footer"]);
        }
        private string ToSettingsString(IGame game)
        {
            if (game.Settings.Count == 0)
                return lang["lobby_no_settings"];

            string settingsString = string.Empty;
            foreach (var Setting in game.Settings)
            {
                settingsString += $"{Setting.Key}: **{Setting.Value.ToString()}**";
            }

            return settingsString;
        }
        public LobbyInvite GetInvite(ulong User, ulong Guild)
        {
            for (int i = 0; i < Invites.Count; i++)
            {
                if (Invites[i].User.Id == User && Invites[i].Lobby.Context.Guild.Id == Guild)
                    return Invites[i];
            }
            return null;
        }
        private bool IsPlayerInGame(ulong User = 0) => GetLobby(User) != null;
        private Lobby GetLobby(ulong User = 0)
        {
            if (User == 0)
                User = Context.User.Id;

            for (int i = 0; i < Lobbies.Count; i++)
            {
                var lobby = Lobbies[i];
                if (lobby.Context.Guild.Id == Context.Guild.Id)
                    for (int j = 0; j < lobby.Players.Count; j++)
                    {
                        if (lobby.Players[j].Id == User)
                            return lobby;
                    }
            }
            return null;
        }
        private string GetGames()
        {
            string GameString = null;
            var Games = Enum.GetNames(typeof(GameType));
            for (int i = 1; i < Games.Length; i++)
            {
                GameString += Games[i] + "\n";
            }
            return GameString;
        }
    }
}