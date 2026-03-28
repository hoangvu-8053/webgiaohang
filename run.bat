@echo off
title Giao Hang Sonic - Running Project
color 0A

echo.
echo  ============================================
echo    GIAO HANG SONIC - KHOI DONG HE THONG
echo  ============================================
echo.

:: Check if backend is already running
netstat -ano | findstr ":5170" > nul
if %errorlevel% equ 0 (
    echo [INFO] Backend da chay tai http://localhost:5170
) else (
    echo [1/2] Khoi dong Backend API...
    start "Backend" cmd /c "cd /d %~dp0backend && dotnet run --urls=http://localhost:5170"
)

:: Check if frontend is already running  
netstat -ano | findstr ":5173" > nul
if %errorlevel% equ 0 (
    echo [INFO] Frontend da chay tai http://localhost:5173
) else (
    echo [2/2] Khoi dong Frontend...
    start "Frontend" cmd /c "cd /d %~dp0frontend && npm run dev"
)

echo.
echo  ============================================
echo    HE THONG DA SAN SANG!
echo  ============================================
echo.
echo    Backend:  http://localhost:5170  (API)
echo    Frontend: http://localhost:5173  (MO APP O DAY)
echo    Swagger:  http://localhost:5170/swagger
echo    Huong dan API: http://localhost:5170/index.html
echo.
echo  Tai khoan test:
echo    Admin:    admin / admin123
echo    Shipper:  shipper3 / shipper123
echo.
echo  ============================================
echo.
echo  Dong cua so nay neu da mo trinh duyet!
echo  Nhan phim bat ky de thoat...
pause > nul
