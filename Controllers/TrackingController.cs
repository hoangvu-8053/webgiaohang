using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;

namespace webgiaohang.Controllers
{
    public class TrackingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tracking
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // POST: Tracking/Search
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string trackingNumber, string customerPhone)
        {
            if (string.IsNullOrEmpty(trackingNumber) && string.IsNullOrEmpty(customerPhone))
            {
                ModelState.AddModelError("", "Vui lòng nhập mã đơn hàng hoặc số điện thoại");
                return View("Index");
            }

            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(trackingNumber))
            {
                query = query.Where(o => o.TrackingNumber == trackingNumber);
            }

            if (!string.IsNullOrEmpty(customerPhone))
            {
                // Tìm theo số điện thoại người gửi hoặc người nhận
                query = query.Where(o => o.SenderPhone == customerPhone || o.ReceiverPhone == customerPhone);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (!orders.Any())
            {
                ModelState.AddModelError("", "Không tìm thấy đơn hàng");
                return View("Index");
            }

            return View("Result", orders);
        }

        // GET: Tracking/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Tracking/MyOrders (cho Sender/Receiver đã đăng nhập)
        [Authorize(Roles = "Sender,Receiver")]
        public async Task<IActionResult> MyOrders()
        {
            var currentUser = User.Identity!.Name;
            var userRole = User.IsInRole("Sender") ? "Sender" : "Receiver";
            
            var orders = new List<Order>();
            
            if (userRole == "Sender")
            {
                orders = await _context.Orders
                    .Where(o => o.CreatedBy == currentUser && o.CreatedByRole == "Sender")
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else if (userRole == "Receiver")
            {
                orders = await _context.Orders
                    .Where(o => o.ReceiverName == currentUser)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            return View(orders);
        }

        // API để cập nhật vị trí đơn hàng (cho shipper)
        [HttpPost]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> UpdateLocation(int orderId, string location)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ShipperName == User.Identity!.Name);

            if (order != null)
            {
                order.CurrentLocation = location;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật vị trí thành công" });
            }

            return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
        }

        // API để lấy thông tin tracking
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrackingInfo(string trackingNumber)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.TrackingNumber == trackingNumber);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var trackingInfo = new
            {
                success = true,
                order = new
                {
                    order.Id,
                    SenderName = order.SenderName,
                    ReceiverName = order.ReceiverName,
                    PickupAddress = order.PickupAddress,
                    DeliveryAddress = order.DeliveryAddress,
                    order.Status,
                    order.OrderDate,
                    order.EstimatedDeliveryDate,
                    order.ActualDeliveryDate,
                    order.CurrentLocation,
                    order.ShipperName,
                    ProductName = order.Product,
                    order.TotalAmount
                }
            };

            return Json(trackingInfo);
        }
    }
} 