using webgiaohang.Models;

namespace webgiaohang.Services
{
    public interface IShipperPaymentService
    {
        /// <summary>
        /// Tính toán số tiền shipper nhận được từ đơn hàng
        /// </summary>
        Task<decimal> CalculateShipperEarningAsync(int orderId, decimal commissionPercent);

        /// <summary>
        /// Tạo bản ghi thanh toán cho shipper khi đơn hàng được giao thành công
        /// </summary>
        Task<ShipperPayment> CreateShipperPaymentAsync(int orderId, string shipperName, decimal commissionPercent);

        /// <summary>
        /// Lấy tất cả thanh toán của một shipper
        /// </summary>
        Task<List<ShipperPayment>> GetShipperPaymentsAsync(string shipperName);

        /// <summary>
        /// Lấy thanh toán theo ID
        /// </summary>
        Task<ShipperPayment?> GetShipperPaymentByIdAsync(int paymentId);

        /// <summary>
        /// Lấy thanh toán theo OrderId
        /// </summary>
        Task<ShipperPayment?> GetShipperPaymentByOrderIdAsync(int orderId);

        /// <summary>
        /// Đánh dấu thanh toán đã được trả cho shipper
        /// </summary>
        Task<bool> MarkAsPaidAsync(int paymentId, string paymentMethod, string? transactionId = null);

        /// <summary>
        /// Tính tổng số tiền chưa thanh toán của một shipper
        /// </summary>
        Task<decimal> GetPendingAmountAsync(string shipperName);

        /// <summary>
        /// Tính tổng số tiền đã thanh toán của một shipper
        /// </summary>
        Task<decimal> GetPaidAmountAsync(string shipperName);

        /// <summary>
        /// Lấy tất cả thanh toán chưa trả (Pending) để admin xử lý
        /// </summary>
        Task<List<ShipperPayment>> GetPendingPaymentsAsync();
    }
}

