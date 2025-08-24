# Web Giao Hàng - Hướng dẫn Triển khai

## Các phương pháp triển khai

### 1. Triển khai lên Railway (Khuyến nghị)

#### Bước 1: Chuẩn bị
1. Tạo tài khoản Railway: https://railway.app/
2. Đăng nhập bằng GitHub account
3. Đảm bảo project đã được push lên GitHub

#### Bước 2: Deploy
1. Vào Railway Dashboard
2. Click "New Project"
3. Chọn "Deploy from GitHub repo"
4. Chọn repository `webgiaohang`
5. Click "Deploy Now"

#### Bước 3: Cấu hình
- Railway sẽ tự động detect .NET project
- Chờ build và deploy (2-3 phút)
- Nhận URL để truy cập web app

### 2. Triển khai bằng Docker

#### Bước 1: Build và chạy
```bash
# Build Docker image
docker build -t webgiaohang .

# Chạy container
docker run -d -p 8080:80 --name webgiaohang-container webgiaohang
```

#### Bước 2: Sử dụng Docker Compose
```bash
# Chạy với docker-compose
docker-compose up -d
```

### 3. Triển khai lên VPS/Server

#### Bước 1: Chuẩn bị server
- Cài đặt .NET 8.0 Runtime
- Cài đặt IIS (Windows) hoặc Nginx (Linux)
- Cài đặt SQL Server hoặc PostgreSQL

#### Bước 2: Publish và deploy
```bash
# Publish project
dotnet publish -c Release -o ./publish

# Copy files lên server
scp -r ./publish/* user@your-server:/var/www/webgiaohang/
```

#### Bước 3: Cấu hình web server
- Cấu hình IIS hoặc Nginx
- Thiết lập reverse proxy
- Cấu hình SSL certificate

### 4. Triển khai lên Render

#### Bước 1: Chuẩn bị
1. Tạo tài khoản Render: https://render.com/
2. Đăng nhập bằng GitHub account

#### Bước 2: Deploy
1. Click "New +" → "Web Service"
2. Connect GitHub repository
3. Chọn repository `webgiaohang`
4. Cấu hình:
   - **Build Command**: `dotnet build`
   - **Start Command**: `dotnet webgiaohang.dll`
5. Click "Create Web Service"

### 5. Triển khai lên Heroku

#### Bước 1: Cài đặt Heroku CLI
```bash
# Cài đặt Heroku CLI
# Tạo app trên Heroku
heroku create webgiaohang-app

# Deploy
git push heroku main
```

## Cấu hình Database

### SQL Server (Production)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=webgiaohang;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
  }
}
```

### PostgreSQL
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-host;Database=webgiaohang;Username=your-user;Password=your-password;"
  }
}
```

## Biến môi trường

Tạo file `.env` hoặc cấu hình trong hosting platform:
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443
PORT=8080
```

## Troubleshooting

### Lỗi thường gặp:
1. **Port đã được sử dụng**: Thay đổi port trong launchSettings.json
2. **Database connection**: Kiểm tra connection string
3. **File permissions**: Đảm bảo quyền ghi cho thư mục wwwroot

### Logs:
```bash
# Xem logs
docker logs webgiaohang-container

# Hoặc trong Railway
railway logs
```

## Bảo mật

1. Thay đổi mật khẩu admin mặc định
2. Cấu hình HTTPS
3. Thiết lập firewall
4. Backup database định kỳ 