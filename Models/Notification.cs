using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? RecipientUsername { get; set; } // null = thông báo cho tất cả
        
        public string? RecipientRole { get; set; } // null = thông báo cho tất cả roles
        
        public string Type { get; set; } = "Info"; // Info, Warning, Error, Success
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? ReadAt { get; set; }
        
        public string? RelatedEntityType { get; set; } // Order, User, etc.
        
        public int? RelatedEntityId { get; set; } // ID của entity liên quan
    }
} 