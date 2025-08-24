#!/bin/bash

# Script deploy Web Giao Hàng cho Linux
ENVIRONMENT=${1:-"Production"}
PORT=${2:-"8080"}

echo "=== Web Giao Hàng - Deploy Script ==="

# Kiểm tra .NET
echo "Kiểm tra .NET..."
dotnet --version

# Restore packages
echo "Restore packages..."
dotnet restore

# Build project
echo "Build project..."
dotnet build -c Release

# Publish project
echo "Publish project..."
dotnet publish -c Release -o ./publish

# Tạo thư mục logs nếu chưa có
mkdir -p ./publish/logs

echo "Deploy thành công!"
echo "Để chạy ứng dụng:"
echo "cd publish"
echo "dotnet webgiaohang.dll --urls http://localhost:$PORT"

# Hỏi có muốn chạy ngay không
read -p "Bạn có muốn chạy ứng dụng ngay bây giờ? (y/n): " runNow
if [[ $runNow == "y" || $runNow == "Y" ]]; then
    echo "Chạy ứng dụng..."
    cd ./publish
    dotnet webgiaohang.dll --urls "http://localhost:$PORT"
fi 