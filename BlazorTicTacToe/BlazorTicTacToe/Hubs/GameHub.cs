using BlazorTicTacToe.Client.Components;
using BlazorTicTacToe.Shared;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Timers;

namespace BlazorTicTacToe.Hubs
{
    public class GameHub : Hub
    {
        private static readonly List<GameRoom> _rooms = new();
        private static readonly Dictionary<string, List<ChatMessage>> _messagesInRooms = new Dictionary<string, List<ChatMessage>>()
        {
            { "Lobby", new List<ChatMessage>() }
        };

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Player with Id '{Context.ConnectionId}' connected.");

            await Clients.Caller.SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r => r.RoomName));
            await Groups.AddToGroupAsync(Context.ConnectionId, "Lobby");

            Process currentProcess = Process.GetCurrentProcess();
            long privateMemory = currentProcess.PrivateMemorySize64;
            long workingSet = currentProcess.WorkingSet64;
            long pagedMemory = currentProcess.PagedMemorySize64;
            long virtualMemory = currentProcess.VirtualMemorySize64;

            Console.WriteLine($"Private Memory: {privateMemory / 1024 / 1024} MB");
            Console.WriteLine($"Working Set: {workingSet / 1024 / 1024} MB");
            Console.WriteLine($"Paged Memory: {pagedMemory / 1024 / 1024} MB");
            Console.WriteLine($"Virtual Memory: {virtualMemory / 1024 / 1024} MB");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var playerId = Context.ConnectionId;
            if (playerId is not null)
            {
                var room = _rooms.FirstOrDefault(x => x.Players.Any(p => p.ConnectionId == playerId));
                if (room is not null)
                {
                    if (room.Players.Count() > 1)
                    {
                        var playerToRemove = room.TryRemovePlayer(playerId);
                        if (playerToRemove is not null)
                        {
                            Console.WriteLine("Player disconnected " + playerId);
                            await Groups.RemoveFromGroupAsync(playerToRemove.ConnectionId, room.RoomId);
                            await Groups.RemoveFromGroupAsync(playerToRemove.ConnectionId, room.RoomId);
                            await Clients.Group(room.RoomId).SendAsync(ConnectionStrings.UpdateGame, room);
                            await Clients.Group("Lobby").SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r => r.RoomName));
                        }

                    }
                    else
                    {
                        _rooms.Remove(room);
                        await Clients.Group("Lobby").SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r => r.RoomName));
                    }
                }
                else
                {
                    await Groups.RemoveFromGroupAsync(playerId, "Lobby");
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<GameRoom?> CreateRoom(string roomName, string playerName)
        {
            if (_rooms.Count() >= 4)
            {
                return null;
            }
            var roomId = Guid.NewGuid().ToString();
            var room = new GameRoom(roomId, roomName);
            _rooms.Add(room);

            var newPlayer = new Player(Context.ConnectionId, playerName);
            room.TryAddPlayer(newPlayer);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Lobby");
            await Clients.Group("Lobby").SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r => r.RoomName));

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
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Lobby");
                    await Clients.Group(roomId).SendAsync(ConnectionStrings.PlayerJoined, newPLayer);
                    await Clients.Group("Lobby").SendAsync(ConnectionStrings.Rooms, _rooms.OrderBy(r => r.RoomName));
                    if (_messagesInRooms.ContainsKey(roomId))
                    {
                        var messageList = _messagesInRooms[roomId];
                        await Clients.Caller.SendAsync(ConnectionStrings.ReceiveMessage, messageList.OrderBy(x => x.SentDate));
                    }
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
            var room = _rooms.FirstOrDefault(x => x.RoomId == roomId);
            if (room is not null)
            {
                room.Game.StartGame();
                await Clients.Group(roomId).SendAsync(ConnectionStrings.UpdateGame, room);
            }

        }

        public async Task MakeMove(string roomId, int row, int col, string playerId)
        {
            var room = _rooms.FirstOrDefault(x => x.RoomId == roomId);
            if (room is not null && room.Game.MakeMove(row, col, playerId))
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

        public async Task MessageSender(string senderId, string senderName, string message, string roomId)
        {
            ChatMessage newChatmessage = new ChatMessage(senderId, senderName, message);
            Console.WriteLine(newChatmessage?.SenderName + " " + newChatmessage?.MessageContent + " " + roomId);

            if (!_messagesInRooms.Keys.Contains(roomId))
            {
                _messagesInRooms.Add(roomId, new List<ChatMessage>());
            }
            var messageList = _messagesInRooms[roomId];
            messageList = _messagesInRooms[roomId];

            if (messageList is not null && newChatmessage is not null)
            {
                if (messageList.Count == 500)
                {
                    messageList.Remove(messageList[0]);
                }
                messageList.Add(newChatmessage);

                await Clients.Group(roomId).SendAsync(ConnectionStrings.ReceiveMessage, messageList.OrderBy(x => x.SentDate));

            }

        }
        public async Task ReceiveChat(string roomId)
        {
            var messageList = _messagesInRooms[roomId];
            messageList = _messagesInRooms[roomId];

            if (messageList is not null)
            {
                await Clients.Group(roomId).SendAsync(ConnectionStrings.ReceiveMessage, messageList.OrderBy(x => x.SentDate));
            }
        }
    }
}
