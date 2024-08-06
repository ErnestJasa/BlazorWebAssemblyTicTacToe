using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTicTacToe.Shared
{
    public class ChatMessage(string senderId, string senderName, string message)
    {
        public string ChatMessageId { get; set; } = new Guid().ToString();
        public string? SenderId { get; set; } = senderId;
        public string? SenderName { get; set; } = senderName;
        public string? MessageContent { get; set; } = message;
    }
}
