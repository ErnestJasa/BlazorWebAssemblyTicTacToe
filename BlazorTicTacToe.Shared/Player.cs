using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTicTacToe.Shared
{
    public class Player(string connectionId, string name)
    {
        public string ConnectionId { get; set; } = connectionId;
        public string RoomId { get; set; }
        public string Name { get; set; } = name;
        public string Symbol { get; set; }

    }
}
