using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore; // Thêm để sử dụng EF Core
using webgiaohang.Data;
using webgiaohang.Models;
using System.Security.Claims; // Thêm để sử dụng ClaimTypes
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace webgiaohang.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Tiêm ApplicationDbContext qua constructor
        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public IActionResult Index()
        {
            if (User.IsInRole("Sender"))
            {
                // Người gửi chỉ thấy đơn hàng mình tạo
                var orders = _context.Orders.Where(o => o.CreatedBy == User.Identity!.Name && o.CreatedByRole == "Sender").ToList();
                return View(orders);
            }
            else if (User.IsInRole("Receiver"))
            {
                // Người nhận chỉ thấy đơn hàng mình sẽ nhận
                var orders = _context.Orders.Where(o => o.ReceiverName == User.Identity!.Name).ToList();
                return View(orders);
            }
            else if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                var orders = _context.Orders.ToList();
                // Nếu là admin hoặc staff, truyền danh sách shipper sang view
                    var shippers = _context.Users.Where(u => u.Role == "Shipper" && u.IsApproved).ToList();
                    ViewBag.Shippers = shippers;
                return View(orders);
            }
            else
            {
                return Forbid();
            }
        }

        // GET: Orders/Create
        [Authorize(Roles = "Admin,Staff,Sender")]
        [HttpGet]
        public IActionResult Create()
        {
            // Nếu là Receiver thì không cho phép truy cập
            if (User.IsInRole("Receiver"))
            {
                return Forbid();
            }
            return View();
        }

        // POST: Orders/Create
        [Authorize(Roles = "Admin,Staff,Sender")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, IFormFile? ProductImage)
        {
            // Nếu là Receiver thì không cho phép tạo đơn
            if (User.IsInRole("Receiver"))
            {
                return Forbid();
            }
            if (ModelState.IsValid)
            {
                order.OrderDate = DateTime.Now;
                order.Status = "Pending";
                order.CreatedBy = User.Identity?.Name ?? "Unknown";
                // Xác định vai trò người tạo
                if (User.IsInRole("Sender"))
                {
                    order.CreatedByRole = "Sender";
                    order.SenderName = User.Identity?.Name ?? "Unknown";
                }
                else
                {
                    order.CreatedByRole = "Admin";
                }
                // Xử lý upload ảnh
                if (ProductImage != null && ProductImage.Length > 0)
                {
                    var fileName = $"order_{DateTime.Now.Ticks}_{Path.GetFileName(ProductImage.FileName)}";
                    var savePath = Path.Combine("wwwroot", "order-images", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await ProductImage.CopyToAsync(stream);
                    }
                    order.ProductImagePath = "/order-images/" + fileName;
                }
                // Tính tổng tiền
                order.TotalAmount = order.Price + order.ShippingFee + (order.InsuranceValue ?? 0);
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Details/5
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = _context.Orders.FirstOrDefault(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            
            // Kiểm tra quyền xem đơn hàng
            if (User.IsInRole("Sender") && order.CreatedBy != User.Identity?.Name)
            {
                return Forbid();
            }
            if (User.IsInRole("Receiver") && order.ReceiverName != User.Identity?.Name)
            {
                return Forbid();
            }
            
            // Kiểm tra xem đơn hàng đã có đánh giá chưa
            var existingReview = _context.Reviews.FirstOrDefault(r => r.OrderId == id);
            ViewBag.HasReview = existingReview != null;
            ViewBag.ExistingReview = existingReview;
            
            return View(order);
        }

        // GET: Orders/Review/5
        [Authorize(Roles = "Sender,Receiver")]
        [HttpGet]
        public IActionResult Review(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền đánh giá
            bool canReview = false;
            if (User.IsInRole("Sender") && order.CreatedBy == User.Identity!.Name)
            {
                canReview = true;
            }
            else if (User.IsInRole("Receiver") && order.ReceiverName == User.Identity!.Name)
            {
                canReview = true;
            }

            if (!canReview)
            {
                return Forbid();
            }

            // Chỉ cho phép đánh giá đơn hàng đã giao thành công
            if (order.Status != "Delivered")
            {
                TempData["Error"] = "Chỉ có thể đánh giá đơn hàng đã giao thành công.";
                return RedirectToAction("Details", new { id = id });
            }

            // Kiểm tra xem đã đánh giá chưa
            var existingReview = _context.Reviews.FirstOrDefault(r => r.OrderId == id);
            if (existingReview != null)
            {
                TempData["Error"] = "Bạn đã đánh giá đơn hàng này rồi.";
                return RedirectToAction("Details", new { id = id });
            }

            return View(new Review { OrderId = id.Value, CustomerName = User.Identity?.Name ?? "Unknown" });
        }

        // POST: Orders/Review
        [Authorize(Roles = "Sender,Receiver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(Review review)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra lại quyền đánh giá
                var order = _context.Orders.FirstOrDefault(o => o.Id == review.OrderId);
                if (order == null || order.Status != "Delivered")
                {
                    TempData["Error"] = "Không thể đánh giá đơn hàng này.";
                    return RedirectToAction("Index");
                }

                bool canReview = false;
                if (User.IsInRole("Sender") && order.CreatedBy == User.Identity?.Name)
                {
                    canReview = true;
                }
                else if (User.IsInRole("Receiver") && order.ReceiverName == User.Identity?.Name)
                {
                    canReview = true;
                }

                if (!canReview)
                {
                    TempData["Error"] = "Bạn không có quyền đánh giá đơn hàng này.";
                    return RedirectToAction("Index");
                }

                review.CustomerName = User.Identity?.Name ?? "Unknown";
                review.ReviewDate = DateTime.Now;
                review.IsActive = true;

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cảm ơn bạn đã đánh giá đơn hàng!";
                return RedirectToAction("Details", new { id = review.OrderId });
            }

            return View(review);
        }

        [HttpGet]
        public IActionResult DebugUser()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return Content("Chưa đăng nhập");
            }
            
            var currentUser = User.Identity.Name;
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            
            var debugInfo = $"User: {currentUser}\n";
            debugInfo += $"Roles: {string.Join(", ", userRoles)}\n";
            debugInfo += $"All Claims: {string.Join("\n", allClaims)}\n";
            debugInfo += $"IsInRole('Shipper'): {User.IsInRole("Shipper")}\n";
            debugInfo += $"IsInRole('Admin'): {User.IsInRole("Admin")}\n";
            
            return Content(debugInfo);
        }

        [HttpGet]
        public async Task<IActionResult> ShipperOrders()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            
            var currentUser = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == currentUser);
            
            // Kiểm tra xem user có phải là shipper đã duyệt không
            if (user == null || user.Role != "Shipper" || !user.IsApproved)
            {
                return Forbid();
            }
            
            // Nếu user chưa có role Shipper trong cookie, refresh authentication
            if (!User.IsInRole("Shipper"))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "Customer")
                };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));
            }
            
            var orders = _context.Orders.Where(o => o.ShipperName == currentUser).ToList();
            return View("IndexShipper", orders);
        }

        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> IndexShipper()
        {
            // Debug: Kiểm tra role của user
            var currentUser = User.Identity!.Name;
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            
            // Kiểm tra xem user có role Shipper không
            if (!User.IsInRole("Shipper"))
            {
                // Kiểm tra xem user có thực sự là Shipper trong database không
                var user = _context.Users.FirstOrDefault(u => u.Username == currentUser);
                if (user != null && user.Role == "Shipper" && user.IsApproved)
                {
                    // Refresh authentication
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username ?? ""),
                        new Claim(ClaimTypes.Role, user.Role ?? "Customer")
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));
                    
                    // Sau khi refresh, lấy lại đơn hàng
                    var refreshedOrders = _context.Orders.Where(o => o.ShipperName == User.Identity!.Name).ToList();
                    return View("IndexShipper", refreshedOrders);
                }
                
                // Log để debug
                System.Diagnostics.Debug.WriteLine($"User {currentUser} không có role Shipper. Roles: {string.Join(", ", userRoles)}");
                return Forbid();
            }
            
            var orders = _context.Orders.Where(o => o.ShipperName == User.Identity!.Name).ToList();
            return View("IndexShipper", orders);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult AssignShipper(int orderId, string shipperName)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                order.ShipperName = shipperName;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Shipper")]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId && o.ShipperName == User.Identity!.Name);
            if (order != null)
            {
                order.Status = status;
                
                // Nếu trạng thái là "Delivered", cập nhật ngày giao hàng thực tế
                if (status == "Delivered")
                {
                    order.ActualDeliveryDate = DateTime.Now;
                }
                
                _context.SaveChanges();
            }
            return RedirectToAction("ShipperOrders");
        }

        // GET: Orders/MyOrders - Trang đơn hàng của khách hàng
        [Authorize(Roles = "Sender,Receiver")]
        public IActionResult MyOrders()
        {
            var currentUser = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = new List<Order>();
            
            if (User.IsInRole("Sender"))
            {
                // Lấy đơn hàng mà user là người gửi
                orders = _context.Orders
                    .Where(o => o.SenderName == currentUser)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
            else if (User.IsInRole("Receiver"))
            {
                // Lấy đơn hàng mà user là người nhận
                orders = _context.Orders
                    .Where(o => o.ReceiverName == currentUser)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }

            ViewBag.UserRole = User.IsInRole("Sender") ? "Sender" : "Receiver";
            return View(orders);
        }

        // GET: Orders/TrackOrder - Theo dõi đơn hàng
        [AllowAnonymous]
        public IActionResult TrackOrder()
        {
            return View();
        }

        // POST: Orders/TrackOrder - Tìm kiếm đơn hàng
        [HttpPost]
        [AllowAnonymous]
        public IActionResult TrackOrder(string trackingNumber, string customerPhone)
        {
            if (string.IsNullOrEmpty(trackingNumber) && string.IsNullOrEmpty(customerPhone))
            {
                ModelState.AddModelError("", "Vui lòng nhập mã theo dõi hoặc số điện thoại");
                return View();
            }

            var order = _context.Orders.FirstOrDefault(o => 
                (o.TrackingNumber == trackingNumber) ||
                (o.SenderPhone == customerPhone) ||
                (o.ReceiverPhone == customerPhone));

            if (order == null)
            {
                ModelState.AddModelError("", "Không tìm thấy đơn hàng với thông tin đã cung cấp");
                return View();
            }

            return RedirectToAction("TrackResult", new { id = order.Id });
        }

        // GET: Orders/TrackResult - Kết quả theo dõi
        [AllowAnonymous]
        public IActionResult TrackResult(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/CancelOrder - Hủy đơn hàng
        [Authorize(Roles = "Sender,Receiver")]
        public IActionResult CancelOrder(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            var currentUser = User.Identity?.Name;
            
            // Kiểm tra quyền hủy đơn hàng
            bool canCancel = false;
            if (User.IsInRole("Sender") && order.SenderName == currentUser)
            {
                canCancel = true;
            }
            else if (User.IsInRole("Receiver") && order.ReceiverName == currentUser)
            {
                canCancel = true;
            }

            if (!canCancel)
            {
                return Forbid();
            }

            // Chỉ cho phép hủy đơn hàng ở trạng thái Pending
            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng ở trạng thái chờ xử lý";
                return RedirectToAction("MyOrders");
            }

            order.Status = "Cancelled";
            _context.SaveChanges();

            TempData["Success"] = "Đã hủy đơn hàng thành công";
            return RedirectToAction("MyOrders");
        }

        // GET: Orders/RequestRefund - Yêu cầu hoàn tiền
        [Authorize(Roles = "Sender,Receiver")]
        public IActionResult RequestRefund(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            var currentUser = User.Identity?.Name;
            
            // Kiểm tra quyền yêu cầu hoàn tiền
            bool canRequest = false;
            if (User.IsInRole("Sender") && order.SenderName == currentUser)
            {
                canRequest = true;
            }
            else if (User.IsInRole("Receiver") && order.ReceiverName == currentUser)
            {
                canRequest = true;
            }

            if (!canRequest)
            {
                return Forbid();
            }

            // Chỉ cho phép yêu cầu hoàn tiền cho đơn hàng đã giao
            if (order.Status != "Delivered")
            {
                TempData["Error"] = "Chỉ có thể yêu cầu hoàn tiền cho đơn hàng đã giao";
                return RedirectToAction("MyOrders");
            }

            // Kiểm tra xem đã yêu cầu hoàn tiền chưa
            if (order.Status == "RefundRequested")
            {
                TempData["Error"] = "Đơn hàng này đã được yêu cầu hoàn tiền";
                return RedirectToAction("MyOrders");
            }

            order.Status = "RefundRequested";
            _context.SaveChanges();

            TempData["Success"] = "Đã gửi yêu cầu hoàn tiền thành công. Chúng tôi sẽ xử lý trong thời gian sớm nhất";
            return RedirectToAction("MyOrders");
        }

        // GET: Orders/OrderHistory - Lịch sử đơn hàng
        [Authorize(Roles = "Sender,Receiver")]
        public IActionResult OrderHistory(string status = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var currentUser = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Orders.AsQueryable();
            
            if (User.IsInRole("Sender"))
            {
                query = query.Where(o => o.CreatedBy == currentUser && o.CreatedByRole == "Sender");
            }
            else if (User.IsInRole("Receiver"))
            {
                query = query.Where(o => o.ReceiverName == currentUser);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Giao không thành công")
                {
                    // Hiển thị đơn hàng có trạng thái "Failed" hoặc "Cancelled"
                    query = query.Where(o => o.Status == "Failed" || o.Status == "Cancelled" || o.Status == "Failed");
                }
                else
                {
                    query = query.Where(o => o.Status == status);
                }
            }

            // Lọc theo ngày
            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= toDate.Value);
            }

            var orders = query.OrderByDescending(o => o.OrderDate).ToList();

            // Debug: Log số lượng đơn hàng tìm được
            System.Diagnostics.Debug.WriteLine($"Found {orders.Count} orders for user {currentUser} with status filter: {status}");

            ViewBag.Status = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.UserRole = User.IsInRole("Sender") ? "Sender" : "Receiver";

            return View("OrderHistory", orders);
        }

        // GET: Orders/OrderStatistics - Thống kê đơn hàng
        [Authorize(Roles = "Sender,Receiver")]
        public IActionResult OrderStatistics()
        {
            var currentUser = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Orders.AsQueryable();
            
            if (User.IsInRole("Sender"))
            {
                query = query.Where(o => o.SenderName == currentUser);
            }
            else if (User.IsInRole("Receiver"))
            {
                query = query.Where(o => o.ReceiverName == currentUser);
            }

            var statistics = new
            {
                TotalOrders = query.Count(),
                PendingOrders = query.Count(o => o.Status == "Pending"),
                ShippingOrders = query.Count(o => o.Status == "Shipping"),
                DeliveredOrders = query.Count(o => o.Status == "Delivered"),
                CancelledOrders = query.Count(o => o.Status == "Cancelled"),
                TotalSpent = query.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount),
                AverageOrderValue = query.Where(o => o.Status == "Delivered").Average(o => o.TotalAmount),
                ThisMonthOrders = query.Count(o => o.OrderDate >= DateTime.Now.AddDays(-30)),
                ThisMonthSpent = query.Where(o => o.OrderDate >= DateTime.Now.AddDays(-30) && o.Status == "Delivered").Sum(o => o.TotalAmount)
            };

            ViewBag.UserRole = User.IsInRole("Sender") ? "Sender" : "Receiver";
            return View(statistics);
        }

        // GET: Orders/Edit
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = _context.Orders.FirstOrDefault(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền chỉnh sửa
            var currentUser = User.Identity?.Name;
            bool canEdit = false;
            
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                canEdit = true;
            }
            else if (User.IsInRole("Sender") && order.SenderName == currentUser && order.Status == "Pending")
            {
                canEdit = true;
            }
            else if (User.IsInRole("Receiver") && order.ReceiverName == currentUser && order.Status == "Pending")
            {
                canEdit = true;
            }

            if (!canEdit)
            {
                return Forbid();
            }

            return View(order);
        }

        // POST: Orders/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff,Sender,Receiver")]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = _context.Orders.FirstOrDefault(o => o.Id == id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    // Kiểm tra quyền chỉnh sửa
                    var currentUser = User.Identity?.Name;
                    bool canEdit = false;
                    
                    if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                    {
                        canEdit = true;
                    }
                    else if (User.IsInRole("Sender") && existingOrder.SenderName == currentUser && existingOrder.Status == "Pending")
                    {
                        canEdit = true;
                    }
                    else if (User.IsInRole("Receiver") && existingOrder.ReceiverName == currentUser && existingOrder.Status == "Pending")
                    {
                        canEdit = true;
                    }

                    if (!canEdit)
                    {
                        return Forbid();
                    }

                    // Cập nhật thông tin đơn hàng
                    existingOrder.SenderName = order.SenderName;
                    existingOrder.SenderEmail = order.SenderEmail;
                    existingOrder.SenderPhone = order.SenderPhone;
                    existingOrder.PickupAddress = order.PickupAddress;
                    existingOrder.ReceiverName = order.ReceiverName;
                    existingOrder.ReceiverEmail = order.ReceiverEmail;
                    existingOrder.ReceiverPhone = order.ReceiverPhone;
                    existingOrder.DeliveryAddress = order.DeliveryAddress;
                    existingOrder.Product = order.Product;
                    existingOrder.ProductDescription = order.ProductDescription;
                    existingOrder.Price = order.Price;
                    existingOrder.ShippingFee = order.ShippingFee;
                    existingOrder.TotalAmount = order.Price + order.ShippingFee + (order.InsuranceValue ?? 0);
                    existingOrder.Weight = order.Weight;
                    existingOrder.DeliveryType = order.DeliveryType;
                    existingOrder.IsInsured = order.IsInsured;
                    existingOrder.InsuranceValue = order.InsuranceValue;
                    existingOrder.Notes = order.Notes;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật đơn hàng thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Delete
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = _context.Orders.FirstOrDefault(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = _context.Orders.FirstOrDefault(m => m.Id == id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đơn hàng thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/ShipperHistory - Lịch sử đơn hàng cho shipper (giao thành công và không thành công)
        [Authorize(Roles = "Shipper")]
        public IActionResult ShipperHistory(string status = "")
        {
            var username = User.Identity?.Name;
            var query = _context.Orders.Where(o => o.ShipperName == username && (o.Status == "Delivered" || o.Status == "Cancelled" || o.Status == "Failed"));
            
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Giao không thành công")
                {
                    // Hiển thị đơn hàng có trạng thái "Failed" hoặc "Cancelled"
                    query = query.Where(o => o.Status == "Failed" || o.Status == "Cancelled");
                }
                else
                {
                    query = query.Where(o => o.Status == status);
                }
            }
            
            var orders = query.OrderByDescending(o => o.ActualDeliveryDate).ToList();
            ViewBag.Status = status;
            return View("ShipperHistory", orders);
        }

        [Authorize(Roles = "Sender,Receiver")]
        public IActionResult DebugOrders()
        {
            var currentUser = User.Identity?.Name;
            var allOrders = _context.Orders.ToList();
            var userOrders = _context.Orders.Where(o => o.CreatedBy == currentUser || o.ReceiverName == currentUser).ToList();
            var failedOrders = _context.Orders.Where(o => o.Status == "Failed" || o.Status == "Cancelled").ToList();
            
            ViewBag.AllOrders = allOrders;
            ViewBag.UserOrders = userOrders;
            ViewBag.FailedOrders = failedOrders;
            ViewBag.CurrentUser = currentUser;
            
            return View();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}