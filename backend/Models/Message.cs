using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        // Đơn hàng liên quan (nếu có)
        public int? OrderId { get; set; }
        
        // Người gửi
        [Required]
        [StringLength(100)]
        public string SenderUsername { get; set; } = string.Empty;
        
        // Người nhận
        [Required]
        [StringLength(100)]
        public string ReceiverUsername { get; set; } = string.Empty;
        
        // Nội dung tin nhắn
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
        
        // Thời gian gửi
        public DateTime SentAt { get; set; } = DateTime.Now;
        
        // Đã đọc chưa
        public bool IsRead { get; set; } = false;
        
        // Thời gian đọc
        public DateTime? ReadAt { get; set; }
        
        // Loại tin nhắn: Text, Image, File, System
        [StringLength(20)]
        public string MessageType { get; set; } = "Text";
        
        // Đường dẫn file (nếu là ảnh hoặc file)
        [StringLength(500)]
        public string? FilePath { get; set; }
    }
}

