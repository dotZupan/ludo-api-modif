using System.Collections.Generic;

namespace LudoApi.Models
{
    public class Player : IPlayer
    {
        public Player(string connectionId, Color color, string name)
        {
            ConnectionId = connectionId;
            Color = color;
            Name = name;  // <-- store the friendly name
        }

        public bool IsReady { get; set; }

        public string ConnectionId { get; }

        public Color Color { get; }

        public string Name { get; }  // <-- new property

        public int PreviousDieRoll { get; set; } = -1;

        public IEnumerable<int> Pieces { get; set; } = new[] { -1, -1, -1, -1 };
    }
}
