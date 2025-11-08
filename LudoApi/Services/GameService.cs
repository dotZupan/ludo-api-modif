using System;
using System.Collections.Generic;
using System.Linq;
using LudoApi.Models;

namespace LudoApi.Services
{
    public class GameService : IGameService
    {
        private static readonly Random Random = new Random();

        private IEnumerable<IPlayer> _players = new List<IPlayer>();

        private int _playerTurn;

        private Turn _playerTurnAction;

        public void StartGame(IEnumerable<IPlayer> players)
        {
            _players = players;
            _playerTurnAction = Turn.None;
        }

       public IEnumerable<PossibleMove> GetPossibleMoves(Player player, int dieRoll)
{
    var moves = new List<PossibleMove>();

    int boardSize = 40;      // total squares on main track
    int startIndex = ColorPositions.StartPosition(player.Color);
    var finishPositions = ColorPositions.WinPositions(player.Color).ToList();

    for (int i = 0; i < player.Pieces.Count(); i++)
    {
        int pos = player.Pieces.ElementAt(i);

        // 1. Pawn in base
        if (pos == -1)
        {
            if (dieRoll == 6 && !player.Pieces.Contains(startIndex))
            {
                moves.Add(new PossibleMove
                {
                    PieceIndex = i,
                    From = pos,
                    To = startIndex
                });
            }
            continue;
        }

        // 2. Pawn on main track
        if (pos < boardSize)
        {
            int stepsToFinishEntry = (startIndex + boardSize - 1 - pos + boardSize) % boardSize + 1;

            if (dieRoll > stepsToFinishEntry)
            {
                int finishIndex = dieRoll - stepsToFinishEntry - 1;
                if (finishIndex < finishPositions.Count && !player.Pieces.Contains(finishPositions[finishIndex]))
                {
                    moves.Add(new PossibleMove
                    {
                        PieceIndex = i,
                        From = pos,
                        To = finishPositions[finishIndex],
                        ToFinish = true
                    });
                }
            }
            else
            {
                int target = (pos + dieRoll) % boardSize;
                if (!player.Pieces.Contains(target))
                {
                    moves.Add(new PossibleMove
                    {
                        PieceIndex = i,
                        From = pos,
                        To = target
                    });
                }
            }
            continue;
        }

        // 3. Pawn in finish line
        int currentFinishIndex = finishPositions.IndexOf(pos);
        if (currentFinishIndex >= 0 && currentFinishIndex + dieRoll < finishPositions.Count &&
            !player.Pieces.Contains(finishPositions[currentFinishIndex + dieRoll]))
        {
            moves.Add(new PossibleMove
            {
                PieceIndex = i,
                From = pos,
                To = finishPositions[currentFinishIndex + dieRoll],
                ToFinish = true
            });
        }
    }

    return moves;
}


        public int RollDie(IPlayer player)
        {
            return player.PreviousDieRoll = Random.Next(1, 7);
        }

        public AdvanceResult Advance(IPlayer player, int pieceIndex)
        {
            var result = new AdvanceResult { PieceIndex = pieceIndex };

            var pieces = player.Pieces.ToArray();
            int from = pieces[pieceIndex];
            int die = player.PreviousDieRoll;
            result.From = from;

            if (die <= 0)
                return result;

            // If in base and rolled a 6, move to start
            if (from == -1)
            {
                pieces[pieceIndex] = ColorPositions.StartPosition(player.Color);
            }
            else
            {
                int next = from + die;
                if (ColorPositions.InFinishLine(player.Color, from))
                {
                    // Already in finish, just advance within finish when not outside winning positions
                    if (!OutsideWinningPosition(player.Color, from)
                        pieces[pieceIndex] = from + die;
                }
                // Check if piece enters finish line
                else if (ColorPositions.IsEnteringFinish(player.Color, from, next))
                {
                    int stepsIntoFinish = next - ColorPositions.HomeEntry(player.Color);
                    pieces[pieceIndex] = ColorPositions.FinishStart(player.Color) + stepsIntoFinish;
                }
                else
                {
                    // Main board wrap
                    if (next >= ColorPositions.BoardSize)
                    {
                        next -= ColorPositions.BoardSize; // wrap back to 0
                    }
                    pieces[pieceIndex] = next;
                }
            }

            result.To = pieces[pieceIndex];
            player.Pieces = pieces;

            // --- Handle kicking ---
            foreach (var opponent in _players.Where(p => p.ConnectionId != player.ConnectionId))
            {
                var opponentPieces = opponent.Pieces.ToArray();

                for (int i = 0; i < opponentPieces.Length; i++)
                {
                    // Only kick on main track (not finish)
                    if (opponentPieces[i] == result.To && opponentPieces[i] < ColorPositions.BoardSize)
                    {
                        opponentPieces[i] = -1; // Back to base
                        result.Kicked.Add((opponent, i));
                    }
                }

                opponent.Pieces = opponentPieces;
            }

            return result;
        }        
public bool HasWon(IPlayer player)
        {
            return player.Pieces.Intersect(ColorPositions.WinPositions(player.Color)).Count() == 4;
        }

        public IPlayer NextTurn()
        {
            switch (_playerTurnAction)
            {
                case Turn.None:
                    _playerTurnAction = Turn.Roll;
                    break;
                case Turn.Roll:
                    _playerTurnAction = Turn.Advance;
                    break;
                case Turn.Advance:
                    _playerTurn = (_playerTurn + 1) % _players.Count();
                    _playerTurnAction = Turn.Roll;
                    break;
                default:
                    throw new Exception("Unknown turn");
            }

            return _players.ElementAt(_playerTurn);
        }

        public IPlayer? GetPlayer(string connectionId)
        {
            return _players.FirstOrDefault(player => player.ConnectionId == connectionId);
        }

        public Turn GetTurn(IPlayer player)
        {
            var index = _players.ToList().IndexOf(player);
            return index == _playerTurn ? _playerTurnAction : Turn.None;
        }
    }
}
