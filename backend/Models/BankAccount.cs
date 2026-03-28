using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class BankAccount
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string BankName { get; set; } = string.Empty; // Tên ngân hàng
        
        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty; // Số tài khoản
        
        [Required]
        [StringLength(100)]
        public string AccountName { get; set; } = string.Empty; // Tên chủ tài khoản
        
        [StringLength(200)]
        public string? Branch { get; set; } // Chi nhánh
        
        [StringLength(50)]
        public string? QRCodeImagePath { get; set; } // Đường dẫn ảnh QR code (nếu có sẵn)
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

