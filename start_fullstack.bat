@echo off
setlocal

set ROOT=%~dp0
set BACKEND=%ROOT%backend
set FRONTEND=%ROOT%frontend

echo ========================================================
echo   SHIPHUB FULLSTACK SYSTEM - KHOI DONG
echo ========================================================

:: Khoi dong Backend
echo [1/2] Dang khoi dong Backend API (dotnet)...
start "Sonic Backend" cmd /k "cd /d %BACKEND% && dotnet run"

:: Cho backend len (5 giay)
timeout /t 5 /nobreak >nul

:: Khoi dong Frontend
echo [2/2] Dang khoi dong Frontend (vite)...
start "Sonic Frontend" cmd /k "cd /d %FRONTEND% && npm run dev"

echo.
echo ========================================================
echo   HE THONG DA KHOI DONG!
echo   - Backend: http://localhost:5170
echo   - Frontend: http://localhost:5173
echo ========================================================
echo.
pause
