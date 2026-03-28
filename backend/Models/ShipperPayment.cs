using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class ShipperPayment
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string ShipperName { get; set; } = string.Empty;
        
        [Required]
        public decimal Amount { get; set; } // Số tiền shipper nhận được
        
        public decimal CommissionPercent { get; set; } // Phần trăm hoa hồng (ví dụ: 0.7 = 70%)
        
        public decimal OrderTotalAmount { get; set; } // Tổng tiền đơn hàng để tính hoa hồng
        
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled
        
        public DateTime? PaidAt { get; set; } // Ngày thanh toán
        
        public string? PaymentMethod { get; set; } // Cash, Bank Transfer, MoMo
        
        public string? TransactionId { get; set; } // ID giao dịch thanh toán
        
        public string? Notes { get; set; } // Ghi chú
        
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày tạo bản ghi
        
        public DateTime? UpdatedAt { get; set; } // Ngày cập nhật
    }
}

