using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class Payment
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        [Required]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Bank Transfer, Credit Card, etc.
        
        [Required]
        public decimal Amount { get; set; }
        
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded
        
        public string? TransactionId { get; set; } // ID giao dịch từ payment gateway
        
        public DateTime? PaidAt { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string? ReceiptNumber { get; set; } // Số biên lai
        
        public string? PaymentProof { get; set; } // Đường dẫn file chứng minh thanh toán
    }
} 