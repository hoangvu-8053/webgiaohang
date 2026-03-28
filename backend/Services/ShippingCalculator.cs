using webgiaohang.Models;

namespace webgiaohang.Services
{
    public interface IShippingCalculator
    {
        decimal Calculate(Order order);
    }

    public class ShippingCalculator : IShippingCalculator
    {
        // Đơn vị: kg, cm, km, VNĐ
        private const decimal VolumetricDivisor = 5000m; // Quy đổi kg: (D*R*C)/5000

        // baseFee: phí cơ bản, perKg: đơn giá theo kg, perKm: đơn giá theo km, freeKm: km miễn phí, minFee: tối thiểu
        private readonly Dictionary<string, (decimal baseFee, decimal perKg, decimal perKm, decimal freeKm, decimal minFee)> _byType = new()
        {
            { "Standard", (baseFee: 15000m, perKg: 8000m,  perKm: 3000m, freeKm: 3m,  minFee: 20000m) },
            { "Express",  (baseFee: 25000m, perKg: 12000m, perKm: 5000m, freeKm: 2m,  minFee: 40000m) },
            { "SameDay",  (baseFee: 40000m, perKg: 20000m, perKm: 7000m, freeKm: 1m,  minFee: 80000m) }
        };

        public decimal Calculate(Order order)
        {
            var type = order.DeliveryType ?? "Standard";
            if (!_byType.TryGetValue(type, out var rule))
            {
                rule = _byType["Standard"];
            }

            var weight = order.Weight ?? 0m; // kg
            var length = order.Length ?? 0m; // cm
            var width  = order.Width  ?? 0m; // cm
            var height = order.Height ?? 0m; // cm
            var distance = order.DistanceKm ?? 0m; // km

            // Cân nặng quy đổi theo thể tích (kg)
            var volumetricWeight = 0m;
            if (length > 0 && width > 0 && height > 0)
            {
                volumetricWeight = (length * width * height) / VolumetricDivisor;
            }

            var billableWeight = Math.Max(weight, volumetricWeight);
            if (billableWeight <= 0)
            {
                billableWeight = 0.5m; // tối thiểu 0.5kg
            }

            var fee = rule.baseFee + rule.perKg * billableWeight;

            // Phụ phí quá khổ: nếu cạnh dài nhất > 150cm hoặc bất kỳ kích thước nào > 100cm => +30k
            var maxEdge = Math.Max(length, Math.Max(width, height));
            if (maxEdge > 150m || (length > 100m || width > 100m || height > 100m))
            {
                fee += 30000m;
            }

            // Phí theo quãng đường (sau km miễn phí)
            var extraKm = Math.Max(0m, distance - rule.freeKm);
            if (extraKm > 0)
            {
                fee += rule.perKm * extraKm;
            }

            // Làm tròn đến 1000đ
            fee = Math.Ceiling(fee / 1000m) * 1000m;

            // Tối thiểu theo loại
            if (fee < rule.minFee)
            {
                fee = rule.minFee;
            }

            return fee;
        }
    }
}

