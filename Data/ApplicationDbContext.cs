using Microsoft.EntityFrameworkCore;
using webgiaohang.Models;
using System;

namespace webgiaohang.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình relationship cho Order và Review
        modelBuilder.Entity<Order>()
            .HasOne<Review>()
            .WithOne(r => r.Order)
            .HasForeignKey<Review>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình relationship cho Order và OrderStatus
        modelBuilder.Entity<OrderStatus>()
            .HasOne(os => os.Order)
            .WithMany()
            .HasForeignKey(os => os.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình relationship cho Order và Payment
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình indexes để tối ưu performance
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.CreatedBy);
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Status);
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderDate);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.RecipientUsername);
        
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.IsRead);
        
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.ShipperName);
        
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.Date);

        // Seed data cho Products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Laptop Dell Inspiron 15",
                Description = "Laptop gaming hiệu năng cao, màn hình 15.6 inch",
                Price = 25000000,
                StockQuantity = 10,
                Category = "Electronics",
                IsActive = true,
                CreatedDate = new DateTime(2025, 1, 1)
            },
            new Product
            {
                Id = 2,
                Name = "iPhone 15 Pro",
                Description = "Điện thoại thông minh cao cấp, camera 48MP",
                Price = 35000000,
                StockQuantity = 15,
                Category = "Electronics",
                IsActive = true,
                CreatedDate = new DateTime(2025, 1, 1)
            },
            new Product
            {
                Id = 3,
                Name = "Sách 'Đắc Nhân Tâm'",
                Description = "Sách kỹ năng sống bán chạy nhất",
                Price = 150000,
                StockQuantity = 50,
                Category = "Books",
                IsActive = true,
                CreatedDate = new DateTime(2025, 1, 1)
            }
        );
    }
}