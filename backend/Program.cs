using Microsoft.EntityFrameworkCore;
using webgiaohang.Services;
using webgiaohang.Models;
using webgiaohang.Data;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add MVC (vẫn giữ để serve static files nếu cần)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Swagger UI

// CORS - cho phép React frontend kết nối
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Database - SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth: Cookie (web session) + JWT (API/mobile)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/api/Account/login-required";
        options.LogoutPath = "/api/Account/Logout";
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

// Email
builder.Services.Configure<webgiaohang.Models.SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailService, EmailService>();

// Revenue settings
builder.Services.Configure<RevenueSettings>(builder.Configuration.GetSection("Revenue"));

// Google Maps
builder.Services.Configure<GoogleMapsSettings>(builder.Configuration.GetSection("GoogleMaps"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDistanceService, DistanceService>();
builder.Services.AddHttpClient<INominatimGeocodingService, NominatimGeocodingService>();

// Shipping calculator
builder.Services.AddSingleton<IShippingCalculator, ShippingCalculator>();

// Payment service
builder.Services.AddScoped<IPaymentService, PaymentService>();

// QR Code service
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

// Shipper Payment service
builder.Services.AddScoped<IShipperPaymentService, ShipperPaymentService>();

// Notification service
builder.Services.AddScoped<INotificationService, NotificationService>();

// SignalR
builder.Services.AddSignalR();

// Session (cho forgot password flow)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ===== DB INIT & SEED =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Tạo bảng ShipperPayments nếu chưa tồn tại
    try
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ShipperPayments';";
        if (command.ExecuteScalar() == null)
        {
            command.CommandText = @"
                CREATE TABLE ShipperPayments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    ShipperName TEXT NOT NULL,
                    Amount TEXT NOT NULL,
                    CommissionPercent TEXT NOT NULL,
                    OrderTotalAmount TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    PaidAt TEXT,
                    PaymentMethod TEXT,
                    TransactionId TEXT,
                    Notes TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT,
                    FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_ShipperPayments_OrderId ON ShipperPayments(OrderId);
                CREATE INDEX IX_ShipperPayments_ShipperName ON ShipperPayments(ShipperName);
                CREATE INDEX IX_ShipperPayments_Status ON ShipperPayments(Status);";
            command.ExecuteNonQuery();
        }
    }
    catch (Exception ex) { Console.WriteLine($"[WARN] ShipperPayments: {ex.Message}"); }

    // Tạo bảng Messages
    try
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Messages';";
        if (command.ExecuteScalar() == null)
        {
            command.CommandText = @"
                CREATE TABLE Messages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER,
                    SenderUsername TEXT NOT NULL,
                    ReceiverUsername TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    SentAt TEXT NOT NULL,
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    ReadAt TEXT,
                    MessageType TEXT NOT NULL DEFAULT 'Text',
                    FilePath TEXT,
                    FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE SET NULL
                );";
            command.ExecuteNonQuery();
        }
    }
    catch (Exception ex) { Console.WriteLine($"[WARN] Messages: {ex.Message}"); }

    // Tạo bảng Notifications
    try
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Notifications';";
        if (command.ExecuteScalar() == null)
        {
            command.CommandText = @"
                CREATE TABLE Notifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    RecipientUsername TEXT,
                    RecipientRole TEXT,
                    Type TEXT NOT NULL DEFAULT 'Info',
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    ReadAt TEXT,
                    RelatedEntityType TEXT,
                    RelatedEntityId INTEGER
                );";
            command.ExecuteNonQuery();
        }
    }
    catch (Exception ex) { Console.WriteLine($"[WARN] Notifications: {ex.Message}"); }

    // Thêm cột BankQRCode vào Users nếu chưa có
    try
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Users);";
        using var reader = command.ExecuteReader();
        var hasBankQRCode = false;
        while (reader.Read()) { if (reader.GetString(1) == "BankQRCode") { hasBankQRCode = true; break; } }
        reader.Close();
        if (!hasBankQRCode)
        {
            command.CommandText = "ALTER TABLE Users ADD COLUMN BankQRCode TEXT;";
            command.ExecuteNonQuery();
        }
    }
    catch (Exception ex) { Console.WriteLine($"[WARN] BankQRCode column: {ex.Message}"); }

    // Seed admin
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = ComputeSha256("admin123"),
            Role = "Admin",
            IsApproved = true
        });
        db.SaveChanges();
        Console.WriteLine("✓ Đã tạo tài khoản admin/admin123");
    }

    // Fix users có Role rỗng
    var emptyRoleUsers = db.Users.Where(u => string.IsNullOrEmpty(u.Role)).ToList();
    foreach (var u in emptyRoleUsers) u.Role = "Customer";
    if (emptyRoleUsers.Count > 0) db.SaveChanges();

    // Thêm DistanceKm và các cột GPS vào Orders
    try
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info('Orders')";
        var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) existingCols.Add(reader.GetString(1));

        async Task AddCol(string col, string def)
        {
            if (!existingCols.Contains(col))
            {
                cmd.CommandText = $"ALTER TABLE Orders ADD COLUMN {col} {def}";
                await cmd.ExecuteNonQueryAsync();
            }
        }
        await AddCol("DistanceKm", "REAL NULL");
        await AddCol("PickupLat", "REAL NULL");
        await AddCol("PickupLng", "REAL NULL");
        await AddCol("DeliveryLat", "REAL NULL");
        await AddCol("DeliveryLng", "REAL NULL");
        await AddCol("ShipperLat", "REAL NULL");
        await AddCol("ShipperLng", "REAL NULL");
        await AddCol("ShipperLocationUpdatedAt", "TEXT NULL");
    }
    catch (Exception ex) { Console.WriteLine($"[WARN] Orders columns: {ex.Message}"); }

    // Seed shipper3
    if (!db.Users.Any(u => u.Username == "shipper3"))
    {
        db.Users.Add(new User
        {
            Username = "shipper3",
            PasswordHash = ComputeSha256("shipper123"),
            Role = "Shipper",
            IsApproved = true
        });
        db.SaveChanges();
    }

    Console.WriteLine("==== Users trong hệ thống ====");
    foreach (var u in db.Users.ToList())
        Console.WriteLine($"  {u.Username} [{u.Role}] approved={u.IsApproved}");
    Console.WriteLine("==============================");

    string ComputeSha256(string pwd)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(pwd)));
    }
}

// Middlewares
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Giao Hàng Sonic API V1");
});

app.MapGet("/", () => Results.Json(new { 
    Api = "Giao Hàng Sonic", 
    Status = "Healthy",
    Version = "1.0",
    Docs = "/swagger" 
}));

app.UseCors("ReactFrontend");
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// API Routes
app.MapControllers();

// SignalR Hubs
app.MapHub<webgiaohang.Hubs.NotificationHub>("/notificationHub");
app.MapHub<webgiaohang.Hubs.ChatHub>("/chatHub");
app.MapHub<webgiaohang.Hubs.LocationHub>("/locationHub");

app.Run();