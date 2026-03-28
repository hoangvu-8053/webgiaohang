using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class OrderStatus
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        [Required]
        public string Status { get; set; } = string.Empty; // Pending, Processing, Shipped, Delivered, Cancelled
        
        [Required]
        public string UpdatedBy { get; set; } = string.Empty; // Username của người cập nhật
        
        [Required]
        public string UpdatedByRole { get; set; } = string.Empty; // Role của người cập nhật
        
        public string? Notes { get; set; } // Ghi chú khi cập nhật trạng thái
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public string? Location { get; set; } // Vị trí hiện tại của đơn hàng
    }
} 