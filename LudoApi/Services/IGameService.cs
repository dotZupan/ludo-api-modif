using System.Collections.Generic;
using LudoApi.Models;

namespace LudoApi.Services
{
    public interface IGameService
    {
        void StartGame(IEnumerable<IPlayer> players);

        int RollDie(IPlayer player);

        IPlayer NextTurn();

        IPlayer? GetPlayer(string connectionId);

        Turn GetTurn(IPlayer player);

        AdvanceResult Advance(IPlayer player, int piece);

        bool HasWon(IPlayer player);

        IEnumerable<PossibleMove> GetPossibleMoves(Player player, int dieRoll);

        void RemovePlayer(string connectionId);
    }
}
