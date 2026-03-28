using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Role { get; set; } = "Customer"; // Admin, Staff, Shipper, Sender, Receiver
        
        public bool IsApproved { get; set; } = false;
        
        // Thông tin cá nhân
        [StringLength(100)]
        public string? FullName { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(200)]
        public string? Address { get; set; }
        
        // Thông tin bổ sung cho Sender/Receiver
        [StringLength(100)]
        public string? CompanyName { get; set; }
        
        [StringLength(20)]
        public string? TaxCode { get; set; }
        
        // Thông tin cho Shipper
        [StringLength(20)]
        public string? LicensePlate { get; set; }
        
        [StringLength(50)]
        public string? VehicleType { get; set; }
        
        // QR code ngân hàng của shipper (để thanh toán)
        [StringLength(200)]
        public string? BankQRCode { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? LastLoginDate { get; set; }
        
        public bool IsActive { get; set; } = true;

        // Đường dẫn ảnh đại diện
        [StringLength(200)]
        public string? Avatar { get; set; }
    }
} 