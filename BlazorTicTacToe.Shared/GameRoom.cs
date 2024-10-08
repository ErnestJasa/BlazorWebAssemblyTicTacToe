﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTicTacToe.Shared
{
    public class GameRoom
    {
        public string RoomId { get; set; } 
        public string RoomName { get; set; } 
        public string RoomMasterId { get; set; }
        public List<Player> Players { get; set; } = new();
        public TicTacToeGame Game { get; set; } = new();

        public GameRoom(string roomId, string roomName)
        {
            RoomId = roomId;
            RoomName = roomName;
        }
        public GameRoom()
        {
                
        }
        public bool TryAddPlayer(Player player)
        {
            if (Players.Count() < 2 && !Players.Any(p => p.ConnectionId == player.ConnectionId))
            {
                if (Players.Count() == 0)
                {
                    Game.PlayerXId = player.ConnectionId;
                    player.Symbol = "X";
                    ChangeRoomMaster(Game.PlayerXId);
                }
                else if (Players.Count() == 1)
                {
                    Game.PlayerOId = player.ConnectionId;

                    player.Symbol = "O";
                }
                player.RoomId = RoomId;
                Players.Add(player);
                return true;
            }
            return false;
        }

        public bool TryRemovePlayer(string playerId)
        {
            var player = Players.FirstOrDefault(x => x.ConnectionId == playerId);
            if (player != null)
            {
                Players.Remove(player);
                var playerStillInRoom = Players[0];
                if (playerStillInRoom is not null)
                {
                    Game.ResetGame();
                    playerStillInRoom.Symbol = "X";
                    Game.PlayerXId = playerStillInRoom.ConnectionId;
                    ChangeRoomMaster(Game.PlayerXId);
                }
                return true;
            }
            return false;
        }

        public void ChangeRoomMaster(string playerId)
        {
            RoomMasterId = playerId;
        }
    }
}
