using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTicTacToe.Shared
{
    public class ChatMessage
    {
        public string ChatMessageId { get; set; } = Guid.NewGuid().ToString();
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string MessageContent { get; set; }
        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public ChatMessage(string senderId, string senderName, string message)
        {
            SenderId = senderId;
            SenderName = senderName;
            MessageContent = message;
        }
        public ChatMessage()
        {
            
        }
    }
}
