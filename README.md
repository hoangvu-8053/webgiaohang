# Web Giao Hàng - Hướng dẫn Triển khai

## Các phương pháp triển khai

### 1. Triển khai lên Azure (Khuyến nghị)

#### Bước 1: Chuẩn bị
1. Tạo tài khoản Azure
2. Cài đặt Azure CLI
3. Đăng nhập: `az login`

#### Bước 2: Tạo App Service
```bash
# Tạo resource group
az group create --name webgiaohang-rg --location southeastasia

# Tạo App Service Plan
az appservice plan create --name webgiaohang-plan --resource-group webgiaohang-rg --sku B1

# Tạo Web App
az webapp create --name webgiaohang-app --resource-group webgiaohang-rg --plan webgiaohang-plan --runtime "DOTNET|8.0"
```

#### Bước 3: Deploy
```bash
# Publish project
dotnet publish -c Release

# Deploy lên Azure
az webapp deployment source config-zip --resource-group webgiaohang-rg --name webgiaohang-app --src ./bin/Release/net8.0/publish/webgiaohang.zip
```

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

### 4. Triển khai lên Heroku

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

# Hoặc trong Azure
az webapp log tail --name webgiaohang-app --resource-group webgiaohang-rg
```

## Bảo mật

1. Thay đổi mật khẩu admin mặc định
2. Cấu hình HTTPS
3. Thiết lập firewall
4. Backup database định kỳ 