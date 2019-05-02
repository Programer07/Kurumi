using Discord;
using Discord.Commands;
using Kurumi.Modules.Games.LobbyGames;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.LobbyGames.Games
{
    public interface IGame
    {
        int MinPlayers { get; }
        int MaxPlayers { get; }
        Dictionary<string, object> Settings { get; }
        List<ulong> Surrender { get; }
        bool AI { get; }

        Task Start(Lobby lobby, ICommandContext context);
        bool ValidPlayer(IUser user);
        string ToString();
    }

    public class NoneGame : IGame
    {
        public int MinPlayers { get; private set; }
        public int MaxPlayers { get; private set; }
        public Dictionary<string, object> Settings { get; private set; }
        public List<ulong> Surrender { get; private set; }
        public bool AI { get; }

        public NoneGame()
        {
            MinPlayers = 1;
            MaxPlayers = 16;
            Settings = new Dictionary<string, object>();
            Surrender = new List<ulong>();
            AI = true;
        }
        public bool ValidPlayer(IUser user) => true;

        public Task Start(Lobby lobby, ICommandContext context) => throw new Exception("Somehow 'None' got started.");

        public override string ToString() => "lobby_no_game";
    }


    public enum GameType : byte
    {
        None,
        Chess,
        Duel,
        Quiz
    }
}