# 🚀 Deploy Web Giao Hàng lên Railway (Miễn phí)

## 📋 Yêu cầu
- Tài khoản GitHub
- Tài khoản Railway (đăng ký miễn phí)

## 🎯 Bước 1: Chuẩn bị project

### Tạo file railway.json
```json
{
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "dotnet webgiaohang.dll",
    "healthcheckPath": "/",
    "healthcheckTimeout": 100,
    "restartPolicyType": "ON_FAILURE"
  }
}
```

### Tạo file Procfile
```
web: dotnet webgiaohang.dll
```

## 🎯 Bước 2: Đăng ký Railway

1. Truy cập: https://railway.app/
2. Click "Start a New Project"
3. Đăng nhập bằng GitHub
4. Chọn "Deploy from GitHub repo"

## 🎯 Bước 3: Deploy

1. **Connect GitHub repository**
2. **Chọn repository** của bạn
3. **Railway sẽ tự động detect** .NET project
4. **Deploy tự động**

## 🌐 Truy cập

Sau khi deploy thành công, Railway sẽ cung cấp URL dạng:
```
https://webgiaohang-production.up.railway.app
```

## 💰 Chi phí
- **Free tier**: $5 credit/tháng
- **Đủ để chạy web app nhỏ**

## 🔧 Cấu hình Environment Variables

Trong Railway Dashboard:
- `ASPNETCORE_ENVIRONMENT=Production`
- `PORT=8080` 