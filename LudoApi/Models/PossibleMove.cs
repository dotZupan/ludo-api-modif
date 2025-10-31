namespace LudoApi.Models
{
    public class PossibleMove
    {
        public int PieceIndex { get; set; }  // Index of the pawn in Player.Pieces
        public int From { get; set; }        // Current position
        public int To { get; set; }          // Target position
        public bool ToFinish { get; set; }   // True if moving into finish line
    }
}
