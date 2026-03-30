using System.ComponentModel.DataAnnotations;

namespace SafeMind.Data.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        [Required]
        public string SenderId { get; set; } = null!;
        [Required]
        public string ReceiverId { get; set; } = null!;
        [Required]
        public string Message { get; set; } = null!;
        [Required]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}