using System.Collections.Generic;

namespace LudoApi.Models
{
    public interface IPlayer
    {
        string ConnectionId { get; }

        Color Color { get; }

        string Name { get; }  // <-- new property for friendly name

        int PreviousDieRoll { get; set; }
        
        bool IsReady { get; set; }

        IEnumerable<int> Pieces { get; set; }
    }
}
