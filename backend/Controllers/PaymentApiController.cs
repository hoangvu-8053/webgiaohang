using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;
using webgiaohang.Services;
using System.Security.Claims;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IConfiguration _configuration;

        public PaymentApiController(
            ApplicationDbContext context,
            IPaymentService paymentService,
            IQRCodeService qrCodeService,
            IConfiguration configuration)
        {
            _context = context;
            _paymentService = paymentService;
            _qrCodeService = qrCodeService;
            _configuration = configuration;
        }

        // ============================================
        // TẠO THANH TOÁN MỚI
        // POST: api/PaymentApi
        // ============================================
        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var payment = await _paymentService.CreatePaymentAsync(
                    request.OrderId,
                    request.PaymentMethod,
                    request.Amount);

                return Ok(new
                {
                    success = true,
                    message = "Tạo thanh toán thành công",
                    payment = new
                    {
                        payment.Id,
                        payment.OrderId,
                        payment.PaymentMethod,
                        payment.Amount,
                        payment.Status,
                        payment.CreatedAt,
                        payment.Notes
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ============================================
        // LẤY THÔNG TIN THANH TOÁN THEO ORDER ID
        // GET: api/PaymentApi/order/{orderId}
        // ============================================
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetPaymentByOrder(int orderId)
        {
            var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId);
            if (payment == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thanh toán cho đơn hàng này" });
            }

            // Kiểm tra quyền truy cập
            var order = await _context.Orders.FindAsync(orderId);
            var username = User.Identity?.Name;
            bool canAccess = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                            (order != null && order.CreatedBy == username) ||
                            (order != null && order.SenderName == username) ||
                            (order != null && order.ReceiverName == username);

            if (!canAccess)
                return Forbid();

            return Ok(new
            {
                success = true,
                payment = new
                {
                    payment.Id,
                    payment.OrderId,
                    payment.PaymentMethod,
                    payment.Amount,
                    payment.Status,
                    payment.TransactionId,
                    payment.PaidAt,
                    payment.ReceiptNumber,
                    payment.Notes,
                    payment.CreatedAt,
                    payment.PaymentProof
                }
            });
        }

        // ============================================
        // XỬ LÝ THANH TOÁN THÀNH CÔNG (ADMIN)
        // POST: api/PaymentApi/{paymentId}/process
        // ============================================
        [HttpPost("{paymentId}/process")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ProcessPayment(int paymentId, [FromBody] ProcessPaymentRequest request)
        {
            try
            {
                var result = await _paymentService.ProcessPaymentAsync(paymentId, request.TransactionId);
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Không thể xử lý thanh toán" });
                }

                var payment = await _context.Payments.FindAsync(paymentId);
                return Ok(new
                {
                    success = true,
                    message = "Xử lý thanh toán thành công",
                    payment = new
                    {
                        payment.Id,
                        payment.Status,
                        payment.TransactionId,
                        payment.PaidAt,
                        payment.ReceiptNumber
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ============================================
        // XÁC NHẬN THANH TOÁN TIỀN MẶT (NGƯỜI GỬI/NGƯỜI NHẬN)
        // POST: api/PaymentApi/{paymentId}/confirm
        // ============================================
        [HttpPost("{paymentId}/confirm")]
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public async Task<IActionResult> ConfirmCashPayment(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

            // Kiểm tra quyền
            var username = User.Identity?.Name;
            bool canConfirm = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                             (payment.Order != null && payment.Order.CreatedBy == username) ||
                             (payment.Order != null && payment.Order.SenderName == username) ||
                             (payment.Order != null && payment.Order.ReceiverName == username);

            if (!canConfirm)
                return Forbid();

            if (payment.PaymentMethod != "Cash")
                return BadRequest(new { success = false, message = "Chỉ xác nhận được thanh toán tiền mặt" });

            if (payment.Status != "Pending")
                return BadRequest(new { success = false, message = $"Thanh toán đang ở trạng thái: {payment.Status}" });

            payment.Status = "Completed";
            payment.PaidAt = DateTime.Now;
            payment.TransactionId = $"CASH-{DateTime.Now:yyyyMMddHHmmss}-{paymentId}";
            payment.ReceiptNumber = $"RCP{DateTime.Now:yyyyMMdd}{paymentId:D6}";
            payment.Notes = $"Xác nhận thanh toán tiền mặt bởi {username}";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xác nhận thanh toán tiền mặt thành công",
                payment = new
                {
                    payment.Id,
                    payment.Status,
                    payment.TransactionId,
                    payment.PaidAt,
                    payment.ReceiptNumber
                }
            });
        }

        // ============================================
        // HOÀN TIỀN
        // POST: api/PaymentApi/{paymentId}/refund
        // ============================================
        [HttpPost("{paymentId}/refund")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> RefundPayment(int paymentId, [FromBody] RefundPaymentRequest request)
        {
            try
            {
                var result = await _paymentService.RefundPaymentAsync(paymentId, request.Reason);
                if (!result)
                    return BadRequest(new { success = false, message = "Không thể hoàn tiền" });

                return Ok(new { success = true, message = "Hoàn tiền thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // LẤY LỊCH SỬ THANH TOÁN CỦA USER
        // GET: api/PaymentApi/history
        // ============================================
        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var username = User.Identity?.Name;
            var query = _context.Payments
                .Include(p => p.Order)
                .Where(p => p.Order != null &&
                    (p.Order.CreatedBy == username ||
                     p.Order.SenderName == username ||
                     p.Order.ReceiverName == username))
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.OrderId,
                    OrderTrackingNumber = p.Order!.TrackingNumber,
                    OrderProduct = p.Order.Product,
                    p.PaymentMethod,
                    p.Amount,
                    p.Status,
                    p.PaidAt,
                    p.ReceiptNumber,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                payments,
                pagination = new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }

        // ============================================
        // TẠO QR CODE THANH TOÁN
        // GET: api/PaymentApi/{paymentId}/qrcode
        // ============================================
        [HttpGet("{paymentId}/qrcode")]
        public async Task<IActionResult> GetPaymentQRCode(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

            // Kiểm tra quyền
            var username = User.Identity?.Name;
            bool canAccess = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                             (payment.Order != null && payment.Order.CreatedBy == username) ||
                             (payment.Order != null && payment.Order.SenderName == username) ||
                             (payment.Order != null && payment.Order.ReceiverName == username);

            if (!canAccess)
                return Forbid();

            try
            {
                string qrCodeUrl = "";

                // Lấy thông tin ngân hàng từ cấu hình
                var bankAccount = _configuration.GetSection("BankAccount").Get<BankAccountSettings>()
                    ?? new BankAccountSettings();
                var bankName = bankAccount.BankName ?? "MB Bank";
                var accountNumber = bankAccount.AccountNumber ?? "";
                var accountName = bankAccount.AccountName ?? "VO HOANG VU";

                // Tạo nội dung thanh toán
                var paymentContent = $"Thanh toan don hang #{payment.OrderId} - {payment.Order?.TrackingNumber}";

                if (payment.PaymentMethod == "MoMo")
                {
                    var momoPhone = _configuration["MoMo:PhoneNumber"] ?? "0703076547";
                    qrCodeUrl = _qrCodeService.GenerateMoMoQRCode(momoPhone, accountName, payment.Amount, paymentContent);
                }
                else if (payment.PaymentMethod == "Bank Transfer")
                {
                    qrCodeUrl = _qrCodeService.GenerateVietQRCode(accountNumber, accountName, payment.Amount, paymentContent);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Phương thức thanh toán không hỗ trợ QR code" });
                }

                return Ok(new
                {
                    success = true,
                    qrCodeUrl,
                    paymentInfo = new
                    {
                        payment.Id,
                        payment.Amount,
                        payment.PaymentMethod,
                        bankName,
                        accountNumber,
                        accountName,
                        content = paymentContent
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi tạo QR: " + ex.Message });
            }
        }

        // ============================================
        // LẤY THÔNG TIN NGÂN HÀNG ĐỂ HIỂN THỊ
        // GET: api/PaymentApi/bank-info
        // ============================================
        [HttpGet("bank-info")]
        [AllowAnonymous]
        public IActionResult GetBankInfo()
        {
            var bankAccount = _configuration.GetSection("BankAccount").Get<BankAccountSettings>()
                ?? new BankAccountSettings();
            var momoPhone = _configuration["MoMo:PhoneNumber"] ?? "0703076547";

            return Ok(new
            {
                success = true,
                bankInfo = new
                {
                    bankName = bankAccount.BankName ?? "MB Bank",
                    accountNumber = MaskAccountNumber(bankAccount.AccountNumber ?? ""),
                    accountName = bankAccount.AccountName ?? "VO HOANG VU",
                    momoPhone = MaskPhoneNumber(momoPhone)
                }
            });
        }

        // ============================================
        // LẤY TẤT CẢ THANH TOÁN (ADMIN)
        // GET: api/PaymentApi/all
        // ============================================
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] string? status = null,
            [FromQuery] string? method = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrEmpty(method))
                query = query.Where(p => p.PaymentMethod == method);

            var totalCount = await query.CountAsync();
            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.OrderId,
                    OrderTrackingNumber = p.Order!.TrackingNumber,
                    p.PaymentMethod,
                    p.Amount,
                    p.Status,
                    p.PaidAt,
                    p.ReceiptNumber,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                payments,
                pagination = new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }

        // ============================================
        // TẢI LÊN CHỨNG TỪ THANH TOÁN
        // POST: api/PaymentApi/{paymentId}/upload-proof
        // ============================================
        [HttpPost("{paymentId}/upload-proof")]
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public async Task<IActionResult> UploadPaymentProof(int paymentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn file" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh hoặc PDF" });

            if (file.Length > 5 * 1024 * 1024) // 5MB
                return BadRequest(new { success = false, message = "File quá lớn (tối đa 5MB)" });

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

            // Kiểm tra quyền
            var username = User.Identity?.Name;
            bool canUpload = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                            (payment.Order != null && payment.Order.CreatedBy == username) ||
                            (payment.Order != null && payment.Order.SenderName == username) ||
                            (payment.Order != null && payment.Order.ReceiverName == username);

            if (!canUpload)
                return Forbid();

            try
            {
                var fileName = $"payment_proof_{paymentId}_{DateTime.Now.Ticks}{ext}";
                var path = Path.Combine("wwwroot", "payment-proofs", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                payment.PaymentProof = "/payment-proofs/" + fileName;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Tải lên chứng từ thành công",
                    proofUrl = payment.PaymentProof
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi lưu file: " + ex.Message });
            }
        }

        // ============================================
        // THỐNG KÊ THANH TOÁN (ADMIN)
        // GET: api/PaymentApi/stats
        // ============================================
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPaymentStats()
        {
            var stats = new
            {
                totalPayments = await _context.Payments.CountAsync(),
                pendingPayments = await _context.Payments.CountAsync(p => p.Status == "Pending"),
                completedPayments = await _context.Payments.CountAsync(p => p.Status == "Completed"),
                refundedPayments = await _context.Payments.CountAsync(p => p.Status == "Refunded"),
                totalAmount = await _context.Payments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount),
                pendingAmount = await _context.Payments.Where(p => p.Status == "Pending").SumAsync(p => p.Amount),
                methodBreakdown = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new { method = g.Key, count = g.Count(), amount = g.Sum(p => p.Amount) })
                    .ToListAsync()
            };

            return Ok(new { success = true, stats });
        }

        // Helper methods
        private string MaskAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
                return accountNumber;
            return accountNumber.Substring(0, 4) + new string('*', accountNumber.Length - 8) + accountNumber.Substring(accountNumber.Length - 4);
        }

        private string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
                return phone;
            return phone.Substring(0, 3) + new string('*', phone.Length - 6) + phone.Substring(phone.Length - 3);
        }
    }

    // Request DTOs
    public class CreatePaymentRequest
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, MoMo, Bank Transfer
        public decimal Amount { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public string TransactionId { get; set; } = "";
    }

    public class RefundPaymentRequest
    {
        public string Reason { get; set; } = "";
    }

    public class BankAccountSettings
    {
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public string? QRCodeImagePath { get; set; }
    }
}
