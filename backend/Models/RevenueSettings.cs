namespace webgiaohang.Models
{
    public class RevenueSettings
    {
        // "Flat" or "Percent"
        public string FeeType { get; set; } = "Flat";
        // Fixed amount per eligible order (VND)
        public decimal FlatAmount { get; set; } = 30000m;
        // Percentage of order TotalAmount (0..1), used when FeeType == "Percent"
        public decimal Percent { get; set; } = 0m;
        // Percentage of ShippingFee that shipper receives (0..1), e.g., 0.7 = 70%
        public decimal ShipperCommissionPercent { get; set; } = 0.7m; // Mặc định 70% của ShippingFee
    }
}






