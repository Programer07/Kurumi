using Discord;
using Discord.Commands;
using Kurumi.Modules.Games.Chess;
using Kurumi.Modules.Games.Duel;
using Kurumi.Modules.Games.Quiz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games
{
    public class Lobby
    {
        public readonly ICommandContext Context;
        public readonly List<IUser> Players;
        public GameType SelectedGame;
        public readonly string Name;
        public IGame Game;
        public bool Ingame;

        public Lobby(ICommandContext Context, string Name, GameType? SelectedGame = null)
        {
            this.Name = Name;
            this.Context = Context;
            Players = new List<IUser>
            {
                Context.User
            };

            if (SelectedGame.HasValue)
                SetGame(SelectedGame.Value);
            else
            {
                this.SelectedGame = GameType.None;
                Game = new NoneGame();
            }
        }
        public void SetGame(GameType game)
        {
            switch (game)
            {
                case GameType.Chess:
                    Game = new ChessGame();
                    break;
                case GameType.Duel:
                    Game = new DuelGame();
                    break;
                case GameType.Quiz:
                    Game = new QuizGame();
                    break;
                case GameType.None:
                    Game = new NoneGame();
                    break;
            }
            SelectedGame = game;
        }
        public void RemovePlayer(ulong Player)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].Id == Player)
                {
                    Players.RemoveAt(i);
                    goto Check;
                }
            }

        Check:
            if (Players.Count == 0 || (Players.Count == 1 && Players[0].IsBot))
                LobbyManager.Lobbies.Remove(this);
        }
        public bool ContainsPlayer(ulong Player)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].Id == Player)
                    return true;
            }
            return false;
        }
        public string ToPlayerString()
        {
            string playerString = string.Empty;

            for (int i = 0; i < Players.Count; i++)
            {
                var Player = Players[i];
                if (i == 0)
                    playerString += $":crown:{Player.Username}\n";
                else
                {
                    if (i >= Game.MaxPlayers || (Player.IsBot && !Game.AI) || !Game.ValidPlayer(Player))
                        playerString += $"***{Player.Username}**\n";
                    else
                        playerString += $"{Player.Username}\n";
                }
            }

            if (playerString.Contains("***"))
                playerString += "\n\n\n*: the player can't\njoin the game";

            return playerString;
        }
        public void Start(ICommandContext GameContext)
        {
            Ingame = true;
            var gameTask = Game.Start(this, GameContext);
            Task.Run(async () => {
                while (!gameTask.IsCompleted)
                    await Task.Delay(1000);
                Ingame = false;
            });
        }
        public int IndexOfPlayer(ulong Player)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].Id == Player)
                    return i;
            }
            return -1;
        }
        public List<IUser> GetPlayers()
        {
            List<IUser> pList = new List<IUser>();
            for (int i = 0; i < Players.Count; i++)
            {
                if (pList.Count <= Game.MaxPlayers && (!Players[i].IsBot || Game.AI) && Game.ValidPlayer(Players[i]))
                {
                    pList.Add(Players[i]);
                }
                else if (pList.Count > Game.MaxPlayers)
                    break;
            }
            return pList;
        }
    }

    public class LobbyInvite
    {
        public IUser User { get; set; }
        public bool? Accepted { get; set; }
        public Lobby Lobby { get; set; }
    }
}