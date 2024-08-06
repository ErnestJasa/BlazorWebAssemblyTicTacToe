using BlazorTicTacToe.Client.Components;
using BlazorTicTacToe.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BlazorTicTacToe.Hubs
{
    public class GameHub : Hub
    {
        private static readonly List<GameRoom> _rooms = new();
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Player with Id '{Context.ConnectionId}' connected.");

            await Clients.Caller.SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r=>r.RoomName));
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var playerId = Context.ConnectionId;
            if (playerId is not null)
            {
                var room = _rooms.FirstOrDefault(x => x.Players.Any(p=> p.ConnectionId == playerId));
                if (room is not null)
                {
                    if (room.Players.Count() > 1)
                    {
                        var playerToRemove = room.TryRemovePlayer(playerId);
                        if (playerToRemove is not null)
                        {
                            await Groups.RemoveFromGroupAsync(playerToRemove.ConnectionId, room.RoomId);
                            await Groups.RemoveFromGroupAsync(playerToRemove.ConnectionId, room.RoomId);
                            await Clients.Group(room.RoomId).SendAsync(ConnectionStrings.UpdateGame, room);
                        }
                    }
                    else
                    {
                        _rooms.Remove(room);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<GameRoom> CreateRoom(string roomName, string playerName)
        {
            var roomId = Guid.NewGuid().ToString();
            var room = new GameRoom(roomId, roomName);
            _rooms.Add(room);

            var newPlayer = new Player(Context.ConnectionId, playerName);
            room.TryAddPlayer(newPlayer);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.All.SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r=>r.RoomName));  

            return room;
        }

        public async Task<GameRoom?> JoinRoom(string roomId, string playerName)
        {
            var room = _rooms.FirstOrDefault(r => r.RoomId == roomId);
            if (room is not null)
            {
                var newPLayer = new Player(Context.ConnectionId, playerName);
                if (room.TryAddPlayer(newPLayer))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Group(roomId).SendAsync(ConnectionStrings.PlayerJoined, newPLayer);

                    return room;
                }
            }

            return null;
        }

        public async Task ChangeRoomMaster(string? roomId, string playerId)
        {
            if (roomId is not null)
            {
                var room = _rooms.FirstOrDefault(x => x.RoomId == roomId);
                room?.ChangeRoomMaster(playerId);
                await Clients.Group(roomId).SendAsync(ConnectionStrings.ChangeMaster, playerId);
            }
        }

        public async Task StartGame(string roomId)
        {
            var room = _rooms.FirstOrDefault(x=> x.RoomId == roomId);
            if (room is not null)
            {
                room.Game.StartGame();
                await Clients.Group(roomId).SendAsync(ConnectionStrings.UpdateGame, room);
            }

        }

        public async Task MakeMove(string roomId, int row, int col, string playerId)
        {
            var room = _rooms.FirstOrDefault(x=>x.RoomId == roomId);
            if(room is not null && room.Game.MakeMove(row,col,playerId))
            {
                room.Game.Winner = room.Game.CheckWinner();
                room.Game.IsDraw = room.Game.CheckDraw() && string.IsNullOrEmpty(room.Game.Winner);

                if (!string.IsNullOrEmpty(room.Game.Winner) || room.Game.IsDraw)
                {
                    room.Game.GameOver = true;
                }

                await Clients.Group(roomId).SendAsync(ConnectionStrings.UpdateGame, room);
            }
        }

        public async Task MessageSender(string senderId, string senderName, string message)
        {
            var newChatmessage = new ChatMessage(senderId, senderName, message);
            Console.WriteLine(newChatmessage?.SenderName + " " + newChatmessage?.MessageContent);
            await Clients.All.SendAsync(ConnectionStrings.ReceiveMessage, newChatmessage);
        }
    }
}
