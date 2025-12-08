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
            _playerTurn = 0;
            _playerTurnAction = Turn.None;
        }

        public void RemovePlayer(string connectionId)
    {
        var list = _players.ToList();
        if (!list.Any())
            return;

        // Kto je aktuálne na ťahu (pred zmenou)
        var currentPlayer = list[_playerTurn];
        bool removedIsCurrent = currentPlayer.ConnectionId == connectionId;

        // Index hráča na odstránenie
        var index = list.FindIndex(p => p.ConnectionId == connectionId);
        if (index == -1)
            return;

        Console.WriteLine($"[GameService] Removing player {connectionId} from game turn order.");

        list.RemoveAt(index);

        if (!list.Any())
        {
            // Zostala prázdna hra
            _players = list;
            _playerTurn = 0;
            _playerTurnAction = Turn.None;
            Console.WriteLine("[GameService] No players left in game.");
            return;
        }

        // Uprav index, aby stále ukazoval na „správneho“ hráča
        if (_playerTurn > index)
        {
            _playerTurn--;
        }

        if (_playerTurn >= list.Count)
        {
            // wrap-around
            _playerTurn = 0;
        }

        _players = list;

        if (removedIsCurrent)
        {
            // Hráč na ťahu odišiel → ďalší hráč začne od Turn.Roll
            _playerTurnAction = Turn.None;
        }

        Console.WriteLine($"[GameService] Players after removal: {string.Join(", ", _players.Select(p => p.ConnectionId))}");
        Console.WriteLine($"[GameService] Current index: {_playerTurn}, action: {_playerTurnAction}");
    }

       public IEnumerable<PossibleMove> GetPossibleMoves(Player player, int dieRoll)
{
    var moves = new List<PossibleMove>();

    int boardSize = ColorPositions.BoardSize; // 40
    int startIndex = ColorPositions.StartPosition(player.Color); // where a piece from base appears
    var finishPositions = ColorPositions.WinPositions(player.Color).ToList(); // e.g. [40,41,42,43]

    for (int i = 0; i < player.Pieces.Count(); i++)
    {
        int pos = player.Pieces.ElementAt(i);

        // 1) Pawn in base
        if (pos == -1)
        {
            // can only leave base on a 6 and only if your own piece is not already on the start tile
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

        // 2) Pawn already in finish line (positions >= boardSize)
        if (pos >= boardSize)
        {
            var currentFinishIndex = finishPositions.IndexOf(pos);
            if (currentFinishIndex >= 0)
            {
                int targetIdx = currentFinishIndex + dieRoll;
                // must not overshoot win positions and must not land on your own piece
                if (targetIdx < finishPositions.Count && !player.Pieces.Contains(finishPositions[targetIdx]))
                {
                    moves.Add(new PossibleMove
                    {
                        PieceIndex = i,
                        From = pos,
                        To = finishPositions[targetIdx],
                        ToFinish = true
                    });
                }
            }
            continue;
        }

        // 3) Pawn on main track (0..boardSize-1)
        // compute distance (in steps) from current pos to the home entry (0..boardSize-1)
        int entry = ColorPositions.HomeEntry(player.Color);
        int distanceToEntry = (entry - pos + boardSize) % boardSize;

        // If dieRoll > distanceToEntry -> we enter finish (maybe), compute steps into finish
        if (dieRoll > distanceToEntry)
        {
            int stepsIntoFinish = dieRoll - distanceToEntry - 1; // 0 means first finish slot
            if (stepsIntoFinish < finishPositions.Count)
            {
                int finishPos = finishPositions[stepsIntoFinish];
                if (!player.Pieces.Contains(finishPos))
                {
                    moves.Add(new PossibleMove
                    {
                        PieceIndex = i,
                        From = pos,
                        To = finishPos,
                        ToFinish = true
                    });
                }
            }
            // else overshoots finish => no legal move
        }
        else
        {
            // Normal main-track move with wrap-around
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
                if (ColorPositions.IsInFinishLine(player.Color, from))
                {
                    // Already in finish, just advance within finish when not outside winning positions
                    if (!ColorPositions.OutsideWinningPosition(player.Color, next))
                        pieces[pieceIndex] = next;
                }
                // Check if piece enters finish line
                else if (ColorPositions.IsEnteringFinish(player.Color, from, next))
                {
                    
                        int stepsIntoFinish = next - ColorPositions.HomeEntry(player.Color);
                        next = ColorPositions.FinishStart(player.Color) + stepsIntoFinish - 1;
                        if (!ColorPositions.OutsideWinningPosition(player.Color, next))
                            pieces[pieceIndex] = next;
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
