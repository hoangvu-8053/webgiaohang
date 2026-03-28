using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        // Thông tin người gửi
        [Required]
        [StringLength(100)]
        public string SenderName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string SenderEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string SenderPhone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string PickupAddress { get; set; } = string.Empty;
        
        // Thông tin người nhận
        [Required]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string ReceiverEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string DeliveryAddress { get; set; } = string.Empty;
        
        // Thông tin hàng hóa
        [Required]
        [StringLength(100)]
        public string Product { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? ProductDescription { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal ShippingFee { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }
        
        // Thông tin giao hàng
        [StringLength(50)]
        public string? TrackingNumber { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        
        [StringLength(100)]
        public string? ShipperName { get; set; }
        
        [StringLength(200)]
        public string? CurrentLocation { get; set; }
        
        public DateTime OrderDate { get; set; } = DateTime.Now;
        
        public DateTime? EstimatedDeliveryDate { get; set; }
        
        public DateTime? ActualDeliveryDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Thông tin bổ sung
        public int? ProductId { get; set; }
        
        // Trọng lượng và kích thước (cho tính phí vận chuyển)
        [Range(0, double.MaxValue)]
        public decimal? Weight { get; set; } // kg
        
        [Range(0, double.MaxValue)]
        public decimal? Length { get; set; } // cm
        
        [Range(0, double.MaxValue)]
        public decimal? Width { get; set; } // cm
        
        [Range(0, double.MaxValue)]
        public decimal? Height { get; set; } // cm

        // Quãng đường vận chuyển (km)
        [Range(0, double.MaxValue)]
        public decimal? DistanceKm { get; set; } // km

        // Loại giao hàng
        [StringLength(50)]
        public string DeliveryType { get; set; } = "Standard"; // Standard, Express, SameDay

        // Bảo hiểm hàng hóa
        public bool IsInsured { get; set; } = false;

        [Range(0, double.MaxValue)]
        public decimal? InsuranceValue { get; set; }

        // Người tạo đơn hàng (có thể là người gửi hoặc người nhận)
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        [StringLength(20)]
        public string CreatedByRole { get; set; } = string.Empty; // Sender, Receiver, Admin

        public string? ProductImagePath { get; set; }

        // Tọa độ địa điểm (để ghim bản đồ)
        // Validation được xử lý trong controller để tránh lỗi khi giá trị hợp lệ
        public decimal? PickupLat { get; set; }

        public decimal? PickupLng { get; set; }

        public decimal? DeliveryLat { get; set; }

        public decimal? DeliveryLng { get; set; }

        // Vị trí hiện tại của shipper (real-time tracking)
        public decimal? ShipperLat { get; set; }

        public decimal? ShipperLng { get; set; }

        public DateTime? ShipperLocationUpdatedAt { get; set; }
    }
}