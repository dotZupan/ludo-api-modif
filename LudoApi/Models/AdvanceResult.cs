using System.Collections.Generic;

namespace LudoApi.Models
{
    public class AdvanceResult
    {
        public int PieceIndex { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public List<(IPlayer Player, int PieceIndex)> Kicked { get; set; } = new List<(IPlayer Player, int PieceIndex)>();
    }
}
