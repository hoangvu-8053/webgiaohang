# 🚀 Deploy Web Giao Hàng lên Render (Miễn phí)

## 📋 Yêu cầu
- Tài khoản GitHub
- Tài khoản Render (đăng ký miễn phí)

## 🎯 Bước 1: Chuẩn bị project

### Tạo file render.yaml
```yaml
services:
  - type: web
    name: webgiaohang
    env: dotnet
    buildCommand: dotnet publish -c Release -o ./publish
    startCommand: dotnet publish/webgiaohang.dll
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: PORT
        value: 8080
```

## 🎯 Bước 2: Đăng ký Render

1. Truy cập: https://render.com/
2. Click "Get Started"
3. Đăng nhập bằng GitHub
4. Click "New +" → "Web Service"

## 🎯 Bước 3: Deploy

1. **Connect GitHub repository**
2. **Chọn repository** của bạn
3. **Cấu hình:**
   - **Name**: `webgiaohang`
   - **Environment**: `Docker`
   - **Region**: `Singapore`
   - **Branch**: `main`
4. **Click "Create Web Service"**

## 🌐 Truy cập

Sau khi deploy thành công, Render sẽ cung cấp URL dạng:
```
https://webgiaohang.onrender.com
```

## 💰 Chi phí
- **Free tier**: 750 giờ/tháng
- **Tự động sleep** sau 15 phút không hoạt động
- **Wake up** khi có request

## 🔧 Cấu hình Environment Variables

Trong Render Dashboard:
- `ASPNETCORE_ENVIRONMENT=Production`
- `PORT=8080` 