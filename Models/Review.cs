using System.ComponentModel.DataAnnotations;

namespace webgiaohang.Models
{
    public class Review
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [StringLength(500)]
        public string? Comment { get; set; }
        
        public DateTime ReviewDate { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
} 