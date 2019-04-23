using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Random;
using Kurumi.StartUp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games.Chess
{
    public class ChessGame : IGame
    {
        public int MinPlayers { get; private set; }
        public int MaxPlayers { get; private set; }
        public Dictionary<string, object> Settings { get; private set; }
        public List<ulong> Surrender { get; private set; }
        public bool AI { get; private set; }

        public ChessGame()
        {
            MinPlayers = 2;
            MaxPlayers = 2;
            Settings = new Dictionary<string, object>()
            {
                { "Round Time", (ushort)180 }
            };
            Surrender = new List<ulong>();
            AI = true;
        }
        public bool ValidPlayer(IUser user) => true;


        #region Game
        private ICommandContext Context;
        private ChessBoard Board;
        private int RoundTime;
        public string Move = "";
        public PromotePieces? Promote = PromotePieces.Queen;
        public ulong Player = 0;

        public Task Start(Lobby lobby, ICommandContext GameContext)
        {
            Surrender = new List<ulong>(); //Reset surrenders

            Context = GameContext;
            Board = new ChessBoard();
            RoundTime = (ushort)Settings["Round Time"];

            return Task.Run(async () => 
            {
                try
                {
                    var lang = Language.GetLanguage(Context.Guild);
                    if (RoundTime > 500 || RoundTime < 10)
                    {
                        RoundTime = 180;
                        await GameContext.Channel.SendEmbedAsync(lang["chess_bad_time"]);
                    }

                    IUser Player1 = lobby.Players[0];
                    IUser Player2 = lobby.Players[1];
                    bool Player1Turn = new KurumiRandom().Next(0, 2) == 1;
                    while (Surrender.Count == 0) //While nobody surrenders
                    {
                        IUser CurrentPlayer = Player1Turn ? Player1 : Player2;
                        lang = Language.GetLanguage(Context.Guild);

                        //Send board
                        string BoardLink = Board.BoardLink;
                        await Context.Channel.SendEmbedAsync(lang["chess_board_desc", "PLAYER", (Player1Turn ? Player1 : Player2).Username, "BOARD", BoardLink, "SIDE", Player1Turn ? "White" : "Black"],
                                                             ImageUrl: BoardLink, Footer: lang["chess_footer"]);

                        //Wait for the next move
                        var MoveRes = await GetMove(CurrentPlayer, Player1Turn, Player1Turn ? Player2 : Player1);
                        if (MoveRes.Result == Result.RanOutOfTime)
                        {
                            Board.ResetEnPassant();
                            goto Next;
                        }
                        else if (MoveRes.Result == Result.Fail)
                        {
                            return Task.CompletedTask;
                        }
                        else if (MoveRes.Result == Result.Cancelled)
                        {
                            bool Player2Surrender = Surrender[0] == Player2.Id;
                            await Context.Channel.SendEmbedAsync(lang["chess_checkmate", "WINNER", (Player2Surrender ? Player1 : Player2).Username,
                                                                  "LOSER", (Player2Surrender ? Player2 : Player1).Username], Title: lang["chess_game_title"]);
                            return Task.CompletedTask;
                        }

                        //Move
                        Board.Move(MoveRes.Move[0], MoveRes.Move[1]);

                        //Check
                        if (Board.InCheck(!Player1Turn))
                        {
                            //Checkmate
                            if (Board.Stalemate(!Player1Turn)) //In check + no moves (stalemate) == checkmate
                            {
                                await Context.Channel.SendEmbedAsync(lang["chess_checkmate", "WINNER", (Player1Turn ? Player1 : Player2).Username,
                                                                                              "LOSER", (Player1Turn ? Player2 : Player1).Username], Title: lang["chess_game_title"], ImageUrl: Board.BoardLink);
                                break;
                            }
                            else
                                await Context.Channel.SendEmbedAsync(lang["chess_check"], Title: lang["chess_game_title"]);
                        }
                        //Stalemate
                        else if (Board.Stalemate(!Player1Turn))
                        {
                            await Context.Channel.SendEmbedAsync(lang["chess_stalemate"], Title: lang["chess_game_title"], ImageUrl: Board.BoardLink);
                            break;
                        }

                        //Promote
                        if (Board.CanPoromote(Player1Turn))
                        {
                            if (CurrentPlayer.IsBot)
                                Board.Promote((byte)PromotePieces.Queen, Player1Turn);
                            else
                            {
                                await Context.Channel.SendEmbedAsync(lang["chess_promote", "PLAYER", (Player1Turn ? Player1 : Player2).Username]);
                                Player = (Player1Turn ? Player1 : Player2).Id;
                                Promote = null;
                                int Timer = (RoundTime / 2) * 10;
                                while (Timer != 0 && Promote == null)
                                {
                                    await Task.Delay(100);
                                    Timer--;
                                }

                                if (Timer == 0)
                                    Promote = PromotePieces.Queen;

                                Board.Promote((byte)Promote, Player1Turn);
                            }
                        }


                    Next:
                        Player1Turn = !Player1Turn;
                    }
                    await Utilities.Log(new LogMessage(LogSeverity.Info, "Chess", "success"), Context);
                }
                catch (Exception ex)
                {
                    await Utilities.Log(new LogMessage(LogSeverity.Error, "Chess", null, ex), Context);
                }
                return Task.CompletedTask;
            });
        }

        public async Task<(byte[] Move, Result Result)> GetMove(IUser player, bool White, IUser otherPlayer)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if(player.Id == Program.Bot.DiscordClient.CurrentUser.Id)
                {
                    await Context.Channel.SendEmbedAsync(lang["chess_waiting", "AI", player.Username]);
                    byte[] Move = Board.GetMove(White);
                    return (Move, Result.Success);
                }

                int Timer = RoundTime * 10;
                Player = player.Id;
            Wait:
                Move = null;
                while(Timer != 0 && Move == null && !Surrender.Contains(player.Id) && !Surrender.Contains(otherPlayer.Id))
                {
                    await Task.Delay(100);
                    Timer--;
                }

                if (Move != null)
                {
                    byte[] move = ChessBoard.DecodeMove(Move);
                    if (move[0] < 0 || move[1] < 0 || move[0] > 63 || move[1] > 63)
                        goto Wait;

                    if(White)
                    {
                        if(!Board.ValidWhiteMove(move[0], move[1], out _))
                        {
                            await Context.Channel.SendEmbedAsync(lang["chess_invalid_move"]);
                            goto Wait;
                        }
                    }
                    else
                    {
                        if (!Board.ValidBlackMove(move[0], move[1], out _))
                        {
                            await Context.Channel.SendEmbedAsync(lang["chess_invalid_move"]);
                            goto Wait;
                        }
                    }
                    return (move, Result.Success);
                }
                else if (Timer == 0)
                {
                    Move = "";
                    await Context.Channel.SendEmbedAsync(lang["chess_out_of_time", "PLAYER", player.Username]);
                    return (null, Result.RanOutOfTime);
                }
                else
                {
                    Move = "";
                    return (null, Result.Cancelled);
                }
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Chess", null, ex), Context);
                return (null, Result.Fail);
            }
        }

        public enum Result : byte
        {
            Success,
            Fail,
            RanOutOfTime,
            Cancelled
        }
        public enum PromotePieces : byte
        {
            Pawn,
            Knight,
            Bishop,
            Rook,
            Queen
        }
        #endregion Game

        public override string ToString() => "lobby_chess";
    }
}