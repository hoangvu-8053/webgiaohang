using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;

namespace webgiaohang.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Report
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            // Thống kê tổng quan
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.Where(o => o.Status == "Delivered").SumAsync(o => o.TotalAmount);
            var shippedOrders = await _context.Orders.Where(o => o.Status == "Shipping" || o.Status == "Delivered").CountAsync();
            var totalServiceFee = shippedOrders * 30000; // Phí dịch vụ 30.000đ/đơn hàng đã vận chuyển
            var totalRevenueWithFee = totalRevenue + totalServiceFee;
            var totalCustomers = await _context.Users.Where(u => u.Role == "Customer" && u.IsApproved).CountAsync();
            var totalShippers = await _context.Users.Where(u => u.Role == "Shipper" && u.IsApproved).CountAsync();

            // Thống kê theo tháng
            var monthlyOrders = await _context.Orders
                .Where(o => o.OrderDate >= thisMonth)
                .CountAsync();
            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate >= thisMonth && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);
            var monthlyShippedOrders = await _context.Orders
                .Where(o => o.OrderDate >= thisMonth && (o.Status == "Shipping" || o.Status == "Delivered"))
                .CountAsync();
            var monthlyServiceFee = monthlyShippedOrders * 30000;
            var monthlyRevenueWithFee = monthlyRevenue + monthlyServiceFee;

            // Thống kê theo trạng thái
            var statusStats = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Top shipper
            var topShippers = await _context.Orders
                .Where(o => o.ShipperName != null)
                .GroupBy(o => o.ShipperName)
                .Select(g => new { ShipperName = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .ToListAsync();

            // Top sản phẩm
            var topProducts = await _context.Orders
                .GroupBy(o => o.Product)
                .Select(g => new { Product = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalServiceFee = totalServiceFee;
            ViewBag.TotalRevenueWithFee = totalRevenueWithFee;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalShippers = totalShippers;
            ViewBag.MonthlyOrders = monthlyOrders;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.MonthlyServiceFee = monthlyServiceFee;
            ViewBag.MonthlyRevenueWithFee = monthlyRevenueWithFee;
            ViewBag.StatusStats = statusStats;
            ViewBag.TopShippers = topShippers;
            ViewBag.TopProducts = topProducts;

            return View();
        }

        // GET: Report/Orders
        public async Task<IActionResult> Orders(DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _context.Orders.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Tính toán thống kê với phí dịch vụ
            var totalOrders = orders.Count;
            var totalRevenue = orders.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount);
            var shippedOrders = orders.Where(o => o.Status == "Shipping" || o.Status == "Delivered").Count();
            var totalServiceFee = shippedOrders * 30000; // Phí dịch vụ 30.000đ/đơn hàng đã vận chuyển
            var totalRevenueWithFee = totalRevenue + totalServiceFee;

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Status = status;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalServiceFee = totalServiceFee;
            ViewBag.TotalRevenueWithFee = totalRevenueWithFee;

            return View(orders);
        }

        // GET: Report/Revenue
        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Orders.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value);
            }

            var revenueData = await query
                .Where(o => o.Status == "Delivered")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { 
                    Date = g.Key, 
                    Revenue = g.Sum(o => o.TotalAmount), 
                    OrderCount = g.Count(),
                    ServiceFee = g.Count() * 30000, // Phí dịch vụ 30.000đ/đơn hàng đã giao
                    TotalRevenue = g.Sum(o => o.TotalAmount) + (g.Count() * 30000)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.RevenueData = revenueData;

            return View();
        }

        // GET: Report/Shipper
        public async Task<IActionResult> Shipper()
        {
            var shipperGroups = await _context.Orders
                .Where(o => o.ShipperName != null)
                .GroupBy(o => o.ShipperName)
                .ToListAsync();

            var shipperStats = shipperGroups.Select(g => new
            {
                ShipperName = g.Key,
                TotalOrders = g.Count(),
                DeliveredOrders = g.Count(o => o.Status == "Delivered"),
                ShippedOrders = g.Count(o => o.Status == "Shipping" || o.Status == "Delivered"),
                TotalRevenue = g.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount),
                TotalServiceFee = g.Count(o => o.Status == "Shipping" || o.Status == "Delivered") * 30000, // Phí dịch vụ 30.000đ/đơn hàng đã vận chuyển
                TotalRevenueWithFee = g.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount) + (g.Count(o => o.Status == "Shipping" || o.Status == "Delivered") * 30000),
                AverageDeliveryTime = g.Where(o => o.ActualDeliveryDate.HasValue)
                    .Select(o => (o.ActualDeliveryDate!.Value - o.OrderDate).TotalDays)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderByDescending(x => x.TotalOrders)
            .ToList();

            ViewBag.ShipperStats = shipperStats;
            return View();
        }

        // API để lấy dữ liệu cho biểu đồ
        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .Reverse()
                .ToList();

            var dailyStats = await _context.Orders
                .Where(o => o.OrderDate >= last7Days.First() && o.Status == "Delivered")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var chartData = last7Days.Select(date => new
            {
                Date = date.ToString("dd/MM"),
                Orders = dailyStats.FirstOrDefault(d => d.Date == date)?.Orders ?? 0,
                Revenue = dailyStats.FirstOrDefault(d => d.Date == date)?.Revenue ?? 0
            }).ToList();

            return Json(chartData);
        }
    }
} 