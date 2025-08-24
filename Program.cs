using Microsoft.EntityFrameworkCore;

using webgiaohang.Data;
using webgiaohang.Models;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

var app = builder.Build();

// Seed admin user if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var admin = new User
        {
            Username = "admin",
            PasswordHash = ComputeHash("admin123"),
            Role = "Admin",
            IsApproved = true
        };
        db.Users.Add(admin);
        db.SaveChanges();
    }
    // Local function for hashing
    string ComputeHash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

// Sửa dữ liệu role bị null/rỗng khi khởi động
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var usersNeedFix = db.Users.Where(u => string.IsNullOrEmpty(u.Role)).ToList();
    foreach (var user in usersNeedFix)
    {
        user.Role = "Customer";
    }
    if (usersNeedFix.Count > 0)
    {
        db.SaveChanges();
    }
}

// In ra danh sách user, role, trạng thái duyệt để kiểm tra
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var users = db.Users.ToList();
    Console.WriteLine("==== Danh sách user trong hệ thống ====");
    foreach (var u in users)
    {
        Console.WriteLine($"Id: {u.Id}, Username: {u.Username}, Role: {u.Role}, IsApproved: {u.IsApproved}");
    }
    Console.WriteLine("======================================");
}

// Thêm tài khoản shipper mới
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.Users.Any(u => u.Username == "shipper3"))
    {
        var shipper3 = new User
        {
            Username = "shipper3",
            PasswordHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", // shipper123
            Role = "Shipper",
            IsApproved = true
        };
        db.Users.Add(shipper3);
        db.SaveChanges();
        Console.WriteLine("Đã thêm tài khoản shipper3/shipper123");
    }
}

// Xóa tất cả đơn hàng mẫu cũ nếu có
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var sampleOrders = db.Orders.Where(o => 
        o.CreatedBy == "sender1" || 
        o.CreatedBy == "sender2" || 
        o.CreatedBy == "sender3" || 
        o.CreatedBy == "sender4").ToList();
    
    if (sampleOrders.Any())
    {
        db.Orders.RemoveRange(sampleOrders);
        db.SaveChanges();
        Console.WriteLine($"Đã xóa {sampleOrders.Count} đơn hàng mẫu cũ!");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
