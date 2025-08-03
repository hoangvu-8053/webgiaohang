# 🚀 Deploy Web Giao Hàng lên Heroku (Miễn phí)

## 📋 Yêu cầu
- Tài khoản GitHub
- Tài khoản Heroku (đăng ký miễn phí)

## 🎯 Bước 1: Chuẩn bị project

### Tạo file Procfile
```
web: dotnet webgiaohang.dll
```

### Tạo file appsettings.Production.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=webgiaohang.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 🎯 Bước 2: Đăng ký Heroku

1. Truy cập: https://heroku.com/
2. Click "Sign up"
3. Tạo tài khoản miễn phí

## 🎯 Bước 3: Deploy

### Cách 1: Deploy qua GitHub
1. **Connect GitHub repository**
2. **Enable automatic deploys**
3. **Deploy branch**

### Cách 2: Deploy qua CLI
```bash
# Cài đặt Heroku CLI
# Login
heroku login

# Tạo app
heroku create webgiaohang-app

# Deploy
git push heroku main
```

## 🌐 Truy cập

Sau khi deploy thành công, Heroku sẽ cung cấp URL dạng:
```
https://webgiaohang-app.herokuapp.com
```

## 💰 Chi phí
- **Free tier**: 550-1000 dyno hours/tháng
- **Sleep sau 30 phút** không hoạt động

## 🔧 Cấu hình Environment Variables

```bash
heroku config:set ASPNETCORE_ENVIRONMENT=Production
heroku config:set PORT=8080
``` 