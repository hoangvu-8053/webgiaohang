# Script deploy Web Giao Hàng lên Windows Server
param(
    [string]$ServerIP = "",
    [string]$Username = "administrator",
    [string]$Password = "",
    [string]$Port = "80"
)

Write-Host "=== Web Giao Hàng - Server Deploy Script ===" -ForegroundColor Green

# Kiểm tra tham số
if ([string]::IsNullOrEmpty($ServerIP)) {
    $ServerIP = Read-Host "Nhập IP của server"
}
if ([string]::IsNullOrEmpty($Password)) {
    $Password = Read-Host "Nhập mật khẩu server" -AsSecureString
    $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
}

# Tạo credentials
$SecurePassword = ConvertTo-SecureString $Password -AsPlainText -Force
$Credentials = New-Object System.Management.Automation.PSCredential($Username, $SecurePassword)

Write-Host "Kết nối đến server $ServerIP..." -ForegroundColor Yellow

# Tạo session
$Session = New-PSSession -ComputerName $ServerIP -Credential $Credentials

# Kiểm tra .NET trên server
Write-Host "Kiểm tra .NET trên server..." -ForegroundColor Yellow
Invoke-Command -Session $Session -ScriptBlock {
    dotnet --version
}

# Tạo thư mục trên server
Write-Host "Tạo thư mục trên server..." -ForegroundColor Yellow
Invoke-Command -Session $Session -ScriptBlock {
    New-Item -ItemType Directory -Path "C:\webgiaohang" -Force
}

# Build và publish project
Write-Host "Build và publish project..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

# Copy files lên server
Write-Host "Copy files lên server..." -ForegroundColor Yellow
Copy-Item -Path "./publish/*" -Destination "C:\webgiaohang\" -Recurse -ToSession $Session -Force

# Cài đặt Windows Service
Write-Host "Cài đặt Windows Service..." -ForegroundColor Yellow
Invoke-Command -Session $Session -ScriptBlock {
    # Tạo service
    New-Service -Name "WebGiaoHang" -BinaryPathName "C:\webgiaohang\webgiaohang.exe" -DisplayName "Web Giao Hàng Service" -StartupType Automatic
    
    # Tạo firewall rule
    New-NetFirewallRule -DisplayName "Web Giao Hàng" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
    
    # Start service
    Start-Service -Name "WebGiaoHang"
}

Write-Host "=== Deploy thành công! ===" -ForegroundColor Green
Write-Host "Web app đã được deploy lên: http://$ServerIP" -ForegroundColor Cyan

# Đóng session
Remove-PSSession $Session 