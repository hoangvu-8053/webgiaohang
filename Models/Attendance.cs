using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        
        [Required]
        public required string ShipperName { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public DateTime? CheckInTime { get; set; }
        
        public DateTime? CheckOutTime { get; set; }
        
        public string Status { get; set; } = "Present"; // Present, Absent, Late, Half-day
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 