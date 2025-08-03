# Script deploy Web Giao Hàng
param(
    [string]$Environment = "Production",
    [string]$Port = "8080"
)

Write-Host "=== Web Giao Hàng - Deploy Script ===" -ForegroundColor Green

# Kiểm tra .NET
Write-Host "Kiểm tra .NET..." -ForegroundColor Yellow
dotnet --version

# Restore packages
Write-Host "Restore packages..." -ForegroundColor Yellow
dotnet restore

# Build project
Write-Host "Build project..." -ForegroundColor Yellow
dotnet build -c Release

# Publish project
Write-Host "Publish project..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

# Tạo thư mục logs nếu chưa có
if (!(Test-Path "./publish/logs")) {
    New-Item -ItemType Directory -Path "./publish/logs"
}

Write-Host "Deploy thành công!" -ForegroundColor Green
Write-Host "Để chạy ứng dụng:" -ForegroundColor Cyan
Write-Host "cd publish" -ForegroundColor White
Write-Host "dotnet webgiaohang.dll --urls http://localhost:$Port" -ForegroundColor White

# Hỏi có muốn chạy ngay không
$runNow = Read-Host "Bạn có muốn chạy ứng dụng ngay bây giờ? (y/n)"
if ($runNow -eq "y" -or $runNow -eq "Y") {
    Write-Host "Chạy ứng dụng..." -ForegroundColor Green
    Set-Location "./publish"
    dotnet webgiaohang.dll --urls "http://localhost:$Port"
} 