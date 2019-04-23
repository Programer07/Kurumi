using Kurumi.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Kurumi.Modules.Games.Chess
{
    public class ChessBoard
    {
        //Cache
        public static readonly ConcurrentDictionary<int, int> RatingCache = new ConcurrentDictionary<int, int>(); //Shared between all chess games

        //Constants
        private const ulong FILE_A = 72340172838076673UL;
        private const ulong FILE_AB = 217020518514230019UL;
        private const ulong FILE_GH = 13889313184910721216UL;
        private const ulong FILE_H = 9259542123273814144UL;
        private const ulong RANK_1 = 255UL;
        private const ulong RANK_4 = 1095216660480UL;
        private const ulong RANK_5 = 4278190080UL;
        private const ulong RANK_8 = 18374686479671623680UL;
        private const ulong KING_B7 = 460039UL;
        private const ulong KNIGHT_C6 = 43234889994UL;
        private const byte AI_DEPTH = 4;

        //Bitboards
        private ulong WP = 0, WN = 0, WB = 0, WR = 0, WQ = 0, WK = 0, BP = 0, BN = 0, BB = 0, BR = 0, BQ = 0, BK = 0, EP = 0;
        //Debug view
        private readonly string[,] Board = {
            { "r", "n", "b", "q", "k", "b", "n", "r" },
            { "p", "p", "p", "p", "p", "p", "p", "p" },
            { " ", " ", " ", " ", " ", " ", " ", " " },
            { " ", " ", " ", " ", " ", " ", " ", " " },
            { " ", " ", " ", " ", " ", " ", " ", " " },
            { " ", " ", " ", " ", " ", " ", " ", " " },
            { "P", "P", "P", "P", "P", "P", "P", "P" },
            { "R", "N", "B", "Q", "K", "B", "N", "R" },
        };

        //History
        private readonly Stack<ChessBoard> History = new Stack<ChessBoard>();
        //Castling
        private bool WhiteKingsideCastle = true;
        private bool WhiteQueensideCastle = true;
        private bool BlackKingsideCastle = true;
        private bool BlackQueensideCastle = true;
        //Piece count tracking
        private byte WhitePieces = 16;
        private byte BlackPieces = 16;

        
        public ChessBoard() 
            => GenerateBoard();
        public ChessBoard(ChessBoard board)
        {
            WP = board.WP;
            WN = board.WN;
            WB = board.WB;
            WR = board.WR;
            WQ = board.WQ;
            WK = board.WK;
            BP = board.BP;
            BN = board.BN;
            BB = board.BB;
            BR = board.BR;
            BQ = board.BQ;
            BK = board.BK;
            EP = board.EP;
            WhitePieces = board.WhitePieces;
            BlackPieces = board.BlackPieces;
            WhiteKingsideCastle = board.WhiteKingsideCastle;
            WhiteQueensideCastle = board.WhiteQueensideCastle;
            BlackKingsideCastle = board.BlackKingsideCastle;
            BlackQueensideCastle = board.BlackQueensideCastle;
        }

        public bool ValidWhiteMove(byte StartIndex, byte EndIndex, out bool PieceFound)
        {
            ulong Mask = 1UL << StartIndex;
            bool Valid = false;
            SetupWhiteMove();
            //Pawn
            if ((WP & Mask) != 0)
            {
                int Delta = StartIndex - EndIndex;
                ulong[] Moves = PossibleWhitePawnMoves();
                if(Delta == 7)
                    Valid = ((Moves[0] | EP) & (1UL << EndIndex)) != 0;
                else if (Delta == 9)
                    Valid = ((Moves[1] | EP) & (1UL << EndIndex)) != 0;
                else if (Delta == 8)
                    Valid = (Moves[2] & (1UL << EndIndex)) != 0;
                else if (Delta == 16)
                    Valid = (Moves[3] & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Knight
            else if ((WN & Mask) != 0)
            {
                ulong Moves = KnightMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Bishop
            else if ((WB & Mask) != 0)
            {
                ulong Moves = DiagonalMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Rook
            else if ((WR & Mask) != 0)
            {
                ulong Moves = SlidingMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Queen
            else if ((WQ & Mask) != 0)
            {
                ulong Moves = SlidingMoves(StartIndex) | DiagonalMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //King
            else if ((WK & Mask) != 0)
            {
                ulong Moves = KingMoves(StartIndex);
                if (WhiteKingsideCastle && (OCCUPIED & 6917529027641081856UL) == 0)
                    Moves |= 4611686018427387904UL;
                else if (WhiteQueensideCastle && (OCCUPIED & 1008806316530991104UL) == 0)
                    Moves |= 288230376151711744UL;
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            else
                PieceFound = false;

            if (!Valid)
                return false;

            Move(StartIndex, EndIndex);
            bool validMove = (WK & WhiteCheck()) == 0;
            Undo();
            return validMove;
        }
        public bool ValidBlackMove(byte StartIndex, byte EndIndex, out bool PieceFound)
        {
            ulong Mask = 1UL << StartIndex;
            bool Valid = false;
            SetupBlackMove();
            //Pawn
            if ((BP & Mask) != 0)
            {
                int Delta = EndIndex - StartIndex;
                ulong[] Moves = PossibleBlackPawnMoves();
                if (Delta == 7)
                    Valid = ((Moves[0] | EP) & (1UL << EndIndex)) != 0;
                else if (Delta == 9)
                    Valid = ((Moves[1] | EP) & (1UL << EndIndex)) != 0;
                else if (Delta == 8)
                    Valid = (Moves[2] & (1UL << EndIndex)) != 0;
                else if (Delta == 16)
                    Valid = (Moves[3] & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Knight
            else if ((BN & Mask) != 0)
            {
                ulong Moves = KnightMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Bishop
            else if ((BB & Mask) != 0)
            {
                ulong Moves = DiagonalMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Rook
            else if ((BR & Mask) != 0)
            {
                ulong Moves = SlidingMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //Queen
            else if ((BQ & Mask) != 0)
            {
                ulong Moves = SlidingMoves(StartIndex) | DiagonalMoves(StartIndex);
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            //King
            else if ((BK & Mask) != 0)
            {
                ulong Moves = KingMoves(StartIndex);
                if (BlackKingsideCastle && (OCCUPIED & 96UL) == 0)
                    Moves |= 64UL;
                else if (BlackQueensideCastle && (OCCUPIED & 14UL) == 0)
                    Moves |= 4UL;
                Valid = (Moves & (1UL << EndIndex)) != 0;
                PieceFound = true;
            }
            else
                PieceFound = false;

            if (!Valid)
                return false;

            Move(StartIndex, EndIndex);
            bool validMove = (BK & BlackCheck()) == 0;
            Undo();
            return validMove;
        }
        public void ResetEnPassant() 
            => EP = 0;
        public void Move(byte From, byte To, bool Push = true)
        {
            if (Push)
                History.Push(new ChessBoard(this));
            ulong FromBit = 1UL << From;
            ulong ToBit = 1UL << To;
            ulong Unset = ~ToBit;

            if ((BP & ToBit) != 0 || (BN & ToBit) != 0 || (BB & ToBit) != 0 || (BR & ToBit) != 0 || (BQ & ToBit) != 0)
                BlackPieces--;
            else if ((WP & ToBit) != 0 || (WN & ToBit) != 0 || (WB & ToBit) != 0 || (WR & ToBit) != 0 || (WQ & ToBit) != 0)
                WhitePieces--;

            if ((EP & ToBit) != 0)
            {
                if (EP > RANK_5) //White
                {
                    WP &= (Unset >> 8);
                    WhitePieces--;
                }
                else //Black
                {
                    BP &= (Unset << 8);
                    BlackPieces--;
                }
            }
            EP = 0;
            if ((WP & FromBit) != 0)
            {
                WP &= ~FromBit;
                WP |= ToBit;
                BP &= Unset;
                BN &= Unset;
                BB &= Unset;
                BR &= Unset;
                BQ &= Unset;
                if (From - To == 16)
                {
                    EP = FromBit >> 8;
                }
            }
            else if ((WN & FromBit) != 0)
            {
                WN &= ~FromBit;
                WN |= ToBit;
                BP &= Unset;
                BN &= Unset;
                BB &= Unset;
                BR &= Unset;
                BQ &= Unset;
            }
            else if ((WB & FromBit) != 0)
            {
                WB &= ~FromBit;
                WB |= ToBit;
                BP &= Unset;
                BN &= Unset;
                BB &= Unset;
                BR &= Unset;
                BQ &= Unset;
            }
            else if ((WR & FromBit) != 0)
            {
                WR &= ~FromBit;
                WR |= ToBit;
                BP &= Unset;
                BN &= Unset;
                BB &= Unset;
                BR &= Unset;
                BQ &= Unset;

                if (From == 56)
                    WhiteQueensideCastle = false;
                else if (From == 63)
                    WhiteKingsideCastle = false;
            }
            else if ((WQ & FromBit) != 0)
            {
                WQ &= ~FromBit;
                WQ |= ToBit;
                BP &= Unset;
                BN &= Unset;
                BB &= Unset;
                BR &= Unset;
                BQ &= Unset;
            }
            else if ((WK & FromBit) != 0)
            {
                WK &= ~FromBit;
                WK |= ToBit;
                BP &= Unset;
                BN &= Unset;
                BB &= Unset;
                BR &= Unset;
                BQ &= Unset;

                if (To == 62 && From == 60)
                    Move(63, 61, false);
                else if (To == 58 && From == 60)
                    Move(56, 59, false);

                WhiteKingsideCastle = false;
                WhiteQueensideCastle = false;
            }
            else if ((BP & FromBit) != 0)
            {
                BP &= ~FromBit;
                BP |= ToBit;
                WP &= Unset;
                WN &= Unset;
                WB &= Unset;
                WR &= Unset;
                WQ &= Unset;

                if (To - From == 16)
                {
                    EP = FromBit << 8;
                }
            }
            else if ((BN & FromBit) != 0)
            {
                BN &= ~FromBit;
                BN |= ToBit;
                WP &= Unset;
                WN &= Unset;
                WB &= Unset;
                WR &= Unset;
                WQ &= Unset;
            }
            else if ((BB & FromBit) != 0)
            {
                BB &= ~FromBit;
                BB |= ToBit;
                WP &= Unset;
                WN &= Unset;
                WB &= Unset;
                WR &= Unset;
                WQ &= Unset;
            }
            else if ((BR & FromBit) != 0)
            {
                BR &= ~FromBit;
                BR |= ToBit;
                WP &= Unset;
                WN &= Unset;
                WB &= Unset;
                WR &= Unset;
                WQ &= Unset;

                if (From == 0)
                    BlackQueensideCastle = false;
                else if (From == 7)
                    BlackKingsideCastle = false;
            }
            else if ((BQ & FromBit) != 0)
            {
                BQ &= ~FromBit;
                BQ |= ToBit;
                WP &= Unset;
                WN &= Unset;
                WB &= Unset;
                WR &= Unset;
                WQ &= Unset;
            }
            else if ((BK & FromBit) != 0)
            {
                BK &= ~FromBit;
                BK |= ToBit;
                WP &= Unset;
                WN &= Unset;
                WB &= Unset;
                WR &= Unset;
                WQ &= Unset;

                BlackKingsideCastle = false;
                BlackQueensideCastle = false;

                if (To == 6 && From == 4)
                    Move(7, 5, false);
                else if (To == 2 && From == 4)
                    Move(0, 3, false);
            }
        }
        private void Undo()
        {
            ChessBoard previous = History.Pop();
            WP = previous.WP;
            WN = previous.WN;
            WB = previous.WB;
            WR = previous.WR;
            WQ = previous.WQ;
            WK = previous.WK;
            BP = previous.BP;
            BN = previous.BN;
            BB = previous.BB;
            BR = previous.BR;
            BQ = previous.BQ;
            BK = previous.BK;
            EP = previous.EP;
            WhitePieces = previous.WhitePieces;
            BlackPieces = previous.BlackPieces;
            WhiteKingsideCastle = previous.WhiteKingsideCastle;
            WhiteQueensideCastle = previous.WhiteQueensideCastle;
            BlackKingsideCastle = previous.BlackKingsideCastle;
            BlackQueensideCastle = previous.BlackQueensideCastle;
        }

        #region Masks
        private static readonly ulong[] FileMask = { //Rank 1 => Rank 8
            72340172838076673, 144680345676153346, 289360691352306692, 578721382704613384, 1157442765409226768, 2314885530818453536, 4629771061636907072, 9259542123273814144
        };
        private static readonly ulong[] RankMask = { //File A => File H
            255, 65280, 16711680, 4278190080, 1095216660480, 280375465082880, 71776119061217280, 18374686479671623680
        };
        private static readonly ulong[] DiagonalMask = { //Top left => bottom right
            1, 258, 66052, 16909320, 4328785936, 1108169199648, 283691315109952, 72624976668147840, 145249953336295424, 290499906672525312,
            580999813328273408, 1161999622361579520, 2323998145211531264, 4647714815446351872, 9223372036854775808
        };
        private static readonly ulong[] DiagonalMask2 = { //Top right => bottom left
            128, 32832, 8405024, 2151686160, 550831656968, 141012904183812, 36099303471055874, 9241421688590303745, 4620710844295151872,
            2310355422147575808, 1155177711073755136, 577588855528488960, 288794425616760832, 144396663052566528, 72057594037927936
        };
        #endregion Masks

        #region Moves
        private ulong EMPTY;
        private ulong CAPTURABLE;
        private ulong ENEMY_PIECES;
        private ulong OCCUPIED;

        #region BlackOnlyMoves
        public ulong SetupBlackMove()
        {
            CAPTURABLE = ~(BP | BN | BB | BR | BQ | BK | WK);
            ENEMY_PIECES = WP | WN | WB | WR | WQ;
            OCCUPIED = WP | WN | WB | WR | WQ | WK | BP | BN | BB | BR | BQ | BK;
            EMPTY = ~OCCUPIED;
            return 0;
        }
        public ulong[] PossibleBlackPawnMoves()
        {
            //Left
            ulong LEFT_MOVES = (BP << 7) & ENEMY_PIECES & ~FILE_H;
            //Right
            ulong RIGHT_MOVES = (BP << 9) & ENEMY_PIECES & ~FILE_A;
            //Forward
            ulong FORWARD_MOVES = (BP << 8) & EMPTY;
            //Forward 2
            ulong FORWARD_2_MOVES = (BP << 16) & EMPTY & (EMPTY << 8) & RANK_5;

            return new ulong[4] { RIGHT_MOVES, LEFT_MOVES, FORWARD_MOVES, FORWARD_2_MOVES };
        }
        #endregion BlackOnlyMoves

        #region WhiteOnlyMoves
        public ulong SetupWhiteMove()
        {
            CAPTURABLE = ~(WP | WN | WB | WR | WQ | WK | BK);
            ENEMY_PIECES = BP | BN | BB | BR | BQ;
            OCCUPIED = WP | WN | WB | WR | WQ | WK | BP | BN | BB | BR | BQ | BK;
            EMPTY = ~OCCUPIED;
            return 0;
        }
        public ulong[] PossibleWhitePawnMoves()
        {
            //Right
            ulong RIGHT_MOVES = (WP >> 7) & ENEMY_PIECES & ~FILE_A;
            //Left
            ulong LEFT_MOVES = (WP >> 9) & ENEMY_PIECES & ~FILE_H;
            //Forward
            ulong FORWARD_MOVES = (WP >> 8) & EMPTY;
            //Forward 2
            ulong FORWARD_2_MOVES = (WP >> 16) & EMPTY & (EMPTY >> 8) & RANK_4;

            return new ulong[4] { RIGHT_MOVES, LEFT_MOVES, FORWARD_MOVES, FORWARD_2_MOVES };
        }
        #endregion WhiteOnlyMoves

        public ulong SlidingMoves(int PieceIndex)
        {
            ulong AsBoard = 1UL << PieceIndex;
            ulong Hor = (OCCUPIED - 2 * AsBoard) ^ Reverse(Reverse(OCCUPIED) - 2 * Reverse(AsBoard));
            ulong Temp = OCCUPIED & FileMask[PieceIndex % 8];
            ulong Vert = (Temp - (2 * AsBoard)) ^ Reverse(Reverse(Temp) - (2 * Reverse(AsBoard)));
            return ((Hor & RankMask[PieceIndex / 8]) | (Vert & FileMask[PieceIndex % 8])) & CAPTURABLE;
        }
        public ulong DiagonalMoves(int PieceIndex)
        {
            ulong AsBoard = 1UL << PieceIndex;
            ulong Diagonal = ((OCCUPIED & DiagonalMask[(PieceIndex / 8) + (PieceIndex % 8)]) - (2 * AsBoard)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask[(PieceIndex / 8) + (PieceIndex % 8)]) - (2 * Reverse(AsBoard)));
            ulong Diagonal2 = ((OCCUPIED & DiagonalMask2[(PieceIndex / 8) + 7 - (PieceIndex % 8)]) - (2 * AsBoard)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask2[(PieceIndex / 8) + 7 - (PieceIndex % 8)]) - (2 * Reverse(AsBoard)));
            return ((Diagonal & DiagonalMask[(PieceIndex / 8) + (PieceIndex % 8)]) | (Diagonal2 & DiagonalMask2[(PieceIndex / 8) + 7 - (PieceIndex % 8)])) & CAPTURABLE;
        }
        public ulong KnightMoves(int PieceIndex)
        {
            ulong MOVES;
            if (PieceIndex > 18)
                MOVES = KNIGHT_C6 << (PieceIndex - 18);
            else
                MOVES = KNIGHT_C6 >> (18 - PieceIndex);

            if (PieceIndex % 8 < 4)
                MOVES &= ~FILE_GH & CAPTURABLE;
            else
                MOVES &= ~FILE_AB & CAPTURABLE;

            return MOVES;
        }
        public ulong KingMoves(int PieceIndex)
        {
            ulong MOVES;
            if (PieceIndex > 9)
                MOVES = KING_B7 << (PieceIndex - 9);
            else
                MOVES = KING_B7 >> (9 - PieceIndex);

            if (PieceIndex % 8 < 4)
                MOVES &= ~FILE_GH & CAPTURABLE;
            else
                MOVES &= ~FILE_AB & CAPTURABLE;

            return MOVES;
        }

        private List<byte[]> GetAllWhiteMoves()
        {
            int Count = 0;
            byte _i = 255;
            List<byte[]> Moves = new List<byte[]>();
            for (byte i = 0; i < 64; i++)
            {
                for(byte j = 0; j < 64; j++)
                {
                    if (ValidWhiteMove(i, j, out bool f))
                        Moves.Add(new byte[2] { i, j });
                    
                    if (f && i != _i)
                    {
                        _i = i;
                        Count++;
                        if (WhitePieces == Count)
                            goto YEET;
                    }
                }
            }
            YEET:
            return Moves;
        }
        private List<byte[]> GetAllBlackMoves()
        {
            int Count = 0;
            byte _i = 255;
            List<byte[]> Moves = new List<byte[]>();
            for (byte i = 0; i < 64; i++)
            {
                for (byte j = 0; j < 64; j++)
                {
                    if (ValidBlackMove(i, j, out bool f))
                        Moves.Add(new byte[2] { i, j });

                    if (f && i != _i)
                    {
                        _i = i;
                        Count++;
                        if (BlackPieces == Count)
                            goto YEET;
                    }
                }
            }
            YEET:
            return Moves;
        }
        #endregion Moves

        #region Ai
        private byte[] Ai_Move = null;
        public byte[] GetMove(bool White)
        {
            MinMax(White, AI_DEPTH, -int.MaxValue, int.MaxValue);
            return Ai_Move;
        }
        private int MinMax(bool Max, int depth, int alpha, int beta)
        {
            if (depth == 0)
                return Rate(false);

            List<byte[]> Moves;
            if (Max)
                Moves = GetAllWhiteMoves();
            else
                Moves = GetAllBlackMoves();

            if (Moves.Count == 0) //Checkmate or stalemate
                return Rate(true);

            if (Max)
            {
                int highest = -int.MaxValue;
                for (int i = 0; i < Moves.Count; i++)
                {
                    byte[] move = Moves[i];
                    Move(move[0], move[1]);
                    int val = MinMax(false, depth - 1, alpha, beta);

                    int j = Math.Max(val, highest);
                    if (j != highest)
                    {
                        highest = j;
                        if (depth == AI_DEPTH)
                            Ai_Move = move;
                    }

                    Undo();

                    alpha = Math.Max(alpha, highest);
                    if (beta <= alpha)
                        break;
                }
                return highest;
            }
            else
            {
                int lowest = int.MaxValue;
                for (int i = 0; i < Moves.Count; i++)
                {
                    byte[] move = Moves[i];
                    Move(move[0], move[1]);
                    int val = MinMax(true, depth - 1, alpha, beta);

                    int j = Math.Min(val, lowest);
                    if(j != lowest)
                    {
                        lowest = j;
                        if(depth == AI_DEPTH)
                            Ai_Move = move;
                    }

                    Undo();

                    beta = Math.Min(beta, lowest);
                    if (beta <= alpha)
                        break;
                }
                return lowest;
            }
        }
        public int Rate(bool NoMove)
        {
            int Hash = GetHashCode();

            if (RatingCache.ContainsKey(Hash))
                return RatingCache[Hash];

            int Score = 0;
            byte WhiteBishopCount = 0;
            byte BlackBishopCount = 0;

            //Doing this first because it sets OCCUPIED
            bool Whitecheck = (WhiteCheck() & WK) != 0;
            bool Blackcheck = (BlackCheck() & BK) != 0;

            for (int i = 0; i < 64; i++)
            {
                ulong Mask = 1UL << i;
                if ((OCCUPIED & Mask) == 0)
                    continue;

                //Pawn
                if ((WP & Mask) != 0)
                {
                    Score += 100 + AiData.WhitePawn[i];
                }
                //Knight
                else if ((WN & Mask) != 0)
                {
                    Score += 300 + AiData.WhiteKnight[i];
                }
                //Bishop
                else if ((WB & Mask) != 0)
                {
                    WhiteBishopCount++;
                    Score += 300 + AiData.WhiteBishop[i];
                }
                //Rook
                else if ((WR & Mask) != 0)
                {
                    Score += 500 + AiData.WhiteRook[i];
                }
                //Queen
                else if ((WQ & Mask) != 0)
                {
                    Score += 900 + AiData.WhiteQueen[i];
                }
                //King
                else if ((WK & Mask) != 0)
                {
                    if (Blackcheck)
                        Score += 1100 + AiData.WhiteKing[i];
                }

                //Black

                //Pawn
                else if ((BP & Mask) != 0)
                {
                    Score -= 100 + AiData.BlackPawn[i];
                }
                //Knight
                else if ((BN & Mask) != 0)
                {
                    Score -= 300 + AiData.BlackKnight[i];
                }
                //Bishop
                else if ((BB & Mask) != 0)
                {
                    BlackBishopCount++;
                    Score -= 300 + AiData.BlackBishop[i];
                }
                //Rook
                else if ((BR & Mask) != 0)
                {
                    Score -= 500 + AiData.BlackRook[i];
                }
                //Queen
                else if ((BQ & Mask) != 0)
                {
                    Score -= 900 + AiData.BlackQueen[i];
                }
                //King
                else if ((BK & Mask) != 0)
                {
                    if (Whitecheck)
                        Score -= 1100 + AiData.BlackKing[i];
                }
            }

            if(NoMove) //Checkmate or stalemate
            {
                Score -= Whitecheck ? 90000000 : 0;
                Score += Blackcheck ? 90000000 : 0;
            }

            if (BlackBishopCount == 1)
                Score += 50;
            if (WhiteBishopCount == 1)
                Score -= 50;

            RatingCache.TryAdd(Hash, Score);
            return Score;
        }
        #endregion Ai

        public bool CanPoromote(bool White)
        {
            if (White)
                return (WP & RANK_1) != 0;
            else
                return (WP & RANK_8) != 0;
        }
        public void Promote(byte Piece, bool White) 
        {
            byte BitboardIndex = White ? Piece : (byte)(Piece + 7);
            ulong PromoteBoard = White ? (WP & RANK_1) : (WP & RANK_8);

            if(White)
                WP &= ~PromoteBoard;
            else
                BP &= ~PromoteBoard;

            IndexBitboard(BitboardIndex) |= PromoteBoard;
        }

        private ulong BlackCheck()
        {
            OCCUPIED = WP | WN | WB | WR | WQ | WK | BP | BN | BB | BR | BQ | BK;
            //Pawn
            ulong Defended = (WP >> 7) & ~FILE_A;
            Defended |= (WP >> 9) & ~FILE_H;
            //
            for (int i = 0; i < 64; i++)
            {
                ulong Temp = 0UL;
                ulong Mask = 1UL << i;

                if ((OCCUPIED & Mask) == 0)
                    continue;

                //Knight
                if ((WN & Mask) != 0)
                {
                    if (i > 18)
                        Temp = KNIGHT_C6 << (i - 18);
                    else
                        Temp = KNIGHT_C6 >> (18 - i);

                    if (i % 8 < 4)
                        Temp &= ~FILE_GH;
                    else
                        Temp &= ~FILE_AB;
                }
                //Bishop
                else if ((WB & Mask) != 0)
                {
                    ulong Diagonal = ((OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Reverse(Mask)));
                    ulong Diagonal2 = ((OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Reverse(Mask)));
                    Temp = ((Diagonal & DiagonalMask[(i / 8) + (i % 8)]) | (Diagonal2 & DiagonalMask2[(i / 8) + 7 - (i % 8)]));
                }
                //Rook
                else if ((WR & Mask) != 0)
                {
                    ulong Hor = (OCCUPIED - 2 * Mask) ^ Reverse(Reverse(OCCUPIED) - 2 * Reverse(Mask));
                    ulong temp = OCCUPIED & FileMask[i % 8];
                    ulong Vert = (temp - (2 * Mask)) ^ Reverse(Reverse(temp) - (2 * Reverse(Mask)));
                    Temp = ((Hor & RankMask[i / 8]) | (Vert & FileMask[i % 8]));
                }
                //Queen
                else if ((WQ & Mask) != 0)
                {
                    //Diag
                    ulong Diagonal = ((OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Reverse(Mask)));
                    ulong Diagonal2 = ((OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Reverse(Mask)));
                    Temp = ((Diagonal & DiagonalMask[(i / 8) + (i % 8)]) | (Diagonal2 & DiagonalMask2[(i / 8) + 7 - (i % 8)]));
                    //Hor
                    ulong Hor = (OCCUPIED - 2 * Mask) ^ Reverse(Reverse(OCCUPIED) - 2 * Reverse(Mask));
                    ulong temp = OCCUPIED & FileMask[i % 8];
                    ulong Vert = (temp - (2 * Mask)) ^ Reverse(Reverse(temp) - (2 * Reverse(Mask)));
                    Temp |= ((Hor & RankMask[i / 8]) | (Vert & FileMask[i % 8]));
                }
                //King
                else if ((WK & Mask) != 0)
                {
                    if (i > 9)
                        Temp = KING_B7 << (i - 9);
                    else
                        Temp = KING_B7 >> (9 - i);

                    if (i % 8 < 4)
                        Temp &= ~FILE_GH;
                    else
                        Temp &= ~FILE_AB;
                }
                
                //Merge board
                Defended |= Temp;
            }

            return Defended;
        }
        private ulong WhiteCheck()
        {
            OCCUPIED = WP | WN | WB | WR | WQ | WK | BP | BN | BB | BR | BQ | BK;
            //Pawn
            ulong Defended = (BP << 7) & ~FILE_H;
            Defended |= (BP << 9) & ~FILE_A;
            //
            for (int i = 0; i < 64; i++)
            {
                ulong Temp = 0UL;
                ulong Mask = 1UL << i;

                if ((OCCUPIED & Mask) == 0)
                    continue;

                //Knight
                if ((BN & Mask) != 0)
                {
                    if (i > 18)
                        Temp = KNIGHT_C6 << (i - 18);
                    else
                        Temp = KNIGHT_C6 >> (18 - i);

                    if (i % 8 < 4)
                        Temp &= ~FILE_GH;
                    else
                        Temp &= ~FILE_AB;
                }
                //Bishop
                else if ((BB & Mask) != 0)
                {
                    ulong Diagonal = ((OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Reverse(Mask)));
                    ulong Diagonal2 = ((OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Reverse(Mask)));
                    Temp = ((Diagonal & DiagonalMask[(i / 8) + (i % 8)]) | (Diagonal2 & DiagonalMask2[(i / 8) + 7 - (i % 8)]));
                }
                //Rook
                else if ((BR & Mask) != 0)
                {
                    ulong Hor = (OCCUPIED - 2 * Mask) ^ Reverse(Reverse(OCCUPIED) - 2 * Reverse(Mask));
                    ulong temp = OCCUPIED & FileMask[i % 8];
                    ulong Vert = (temp - (2 * Mask)) ^ Reverse(Reverse(temp) - (2 * Reverse(Mask)));
                    Temp = ((Hor & RankMask[i / 8]) | (Vert & FileMask[i % 8]));
                }
                //Queen
                else if ((BQ & Mask) != 0)
                {
                    //Diag
                    ulong Diagonal = ((OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask[(i / 8) + (i % 8)]) - (2 * Reverse(Mask)));
                    ulong Diagonal2 = ((OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Mask)) ^ Reverse(Reverse(OCCUPIED & DiagonalMask2[(i / 8) + 7 - (i % 8)]) - (2 * Reverse(Mask)));
                    Temp = ((Diagonal & DiagonalMask[(i / 8) + (i % 8)]) | (Diagonal2 & DiagonalMask2[(i / 8) + 7 - (i % 8)]));
                    //Hor
                    ulong Hor = (OCCUPIED - 2 * Mask) ^ Reverse(Reverse(OCCUPIED) - 2 * Reverse(Mask));
                    ulong temp = OCCUPIED & FileMask[i % 8];
                    ulong Vert = (temp - (2 * Mask)) ^ Reverse(Reverse(temp) - (2 * Reverse(Mask)));
                    Temp |= ((Hor & RankMask[i / 8]) | (Vert & FileMask[i % 8]));
                }
                //King
                else if ((BK & Mask) != 0)
                {
                    if (i > 9)
                        Temp = KING_B7 << (i - 9);
                    else
                        Temp = KING_B7 >> (9 - i);

                    if (i % 8 < 4)
                        Temp &= ~FILE_GH;
                    else
                        Temp &= ~FILE_AB;
                }

                //Merge board
                Defended |= Temp;
            }

            return Defended;
        }
        public bool InCheck(bool White)
        {
            if (White)
                return (WhiteCheck() & WK) != 0;
            else
                return (BlackCheck() & BK) != 0;
        }
        public bool Stalemate(bool White) 
            => White ? GetAllWhiteMoves().Count == 0 : GetAllBlackMoves().Count == 0;
        private ulong Reverse(ulong x)
        {
            ulong y = 0;
            for (int i = 0; i < 64; ++i)
            {
                y <<= 1;
                y |= (x & 1);
                x >>= 1;
            }
            return y;
        }
        public string BoardLink 
        {
            get
            {
                string Api = (Config.SSLEnabled ? "https://" : "http://") + "api.kurumibot.moe/api/chessboard/";
                for (byte i = 0; i < 64; i++)
                {
                    if ((WP & (1UL << i)) != 0)
                        Api += "Pa";
                    else if ((WN & (1UL << i)) != 0)
                        Api += "Kn";
                    else if ((WB & (1UL << i)) != 0)
                        Api += "Bi";
                    else if ((WR & (1UL << i)) != 0)
                        Api += "Ro";
                    else if ((WQ & (1UL << i)) != 0)
                        Api += "Qu";
                    else if ((WK & (1UL << i)) != 0)
                        Api += "Ki";
                    else if ((BP & (1UL << i)) != 0)
                        Api += "pa";
                    else if ((BN & (1UL << i)) != 0)
                        Api += "kn";
                    else if ((BB & (1UL << i)) != 0)
                        Api += "bi";
                    else if ((BR & (1UL << i)) != 0)
                        Api += "ro";
                    else if ((BQ & (1UL << i)) != 0)
                        Api += "qu";
                    else if ((BK & (1UL << i)) != 0)
                        Api += "ki";
                    else
                        Api += "-";
                }
                return Api;
            }
        }
        private void GenerateBoard()
        {
            WP = 0; WN = 0; WB = 0; WR = 0; WQ = 0; WK = 0; BP = 0; BN = 0; BB = 0; BR = 0; BQ = 0; BK = 0; EP = 0; //Make VS shut up about making it read only

            for (byte row = 0; row < 8; row++)
            {
                for (byte column = 0; column < 8; column++)
                {
                    int BoardIndex = row * 8 + column;
                    string Piece = Board[row, column];
                    if (Piece != " ")
                        IndexBitboardByType(Piece) |= 1UL << BoardIndex;
                }
            }
        }
        private ref ulong IndexBitboardByType(string piece)
        {
            switch (piece)
            {
                case "P": return ref WP;
                case "N": return ref WN;
                case "B": return ref WB;
                case "R": return ref WR;
                case "Q": return ref WQ;
                case "K": return ref WK;
                case "p": return ref BP;
                case "n": return ref BN;
                case "b": return ref BB;
                case "r": return ref BR;
                case "q": return ref BQ;
                case "k": return ref BK;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
        private ref ulong IndexBitboard(byte index)
        {
            switch (index)
            {
                case 0: return ref WP;
                case 1: return ref WN;
                case 2: return ref WB;
                case 3: return ref WR;
                case 4: return ref WQ;
                case 5: return ref WK;
                case 6: return ref BP;
                case 7: return ref BN;
                case 8: return ref BB;
                case 9: return ref BR;
                case 10: return ref BQ;
                case 11: return ref BK;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        #region MoveDecode
        public static byte[] DecodeMove(string Move)
        {
            string From = Move.Substring(0, 2);
            string To = Move.Substring(3, 2);

            return new byte[2] { GetIndex(From), GetIndex(To) };
        }
        private static byte GetIndex(string Move)
        {
            char[] ABC = "ABCDEFGH".ToCharArray();
            byte Row = byte.Parse(Move[1].ToString());
            byte Column = (byte)Array.IndexOf(ABC, Move[0]);

            return (byte)((Row - 1) * 8 + Column);
        }
        #endregion MoveDecode

        public override bool Equals(object obj)
        {
            return obj is ChessBoard board &&
                   WP == board.WP &&
                   WN == board.WN &&
                   WB == board.WB &&
                   WR == board.WR &&
                   WQ == board.WQ &&
                   WK == board.WK &&
                   BP == board.BP &&
                   BN == board.BN &&
                   BB == board.BB &&
                   BR == board.BR &&
                   BQ == board.BQ &&
                   BK == board.BK;
        }
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(WP);
            hash.Add(WN);
            hash.Add(WB);
            hash.Add(WR);
            hash.Add(WQ);
            hash.Add(WK);
            hash.Add(BP);
            hash.Add(BN);
            hash.Add(BB);
            hash.Add(BR);
            hash.Add(BQ);
            hash.Add(BK);
            return hash.ToHashCode();
        }
    }
}