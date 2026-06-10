@echo off
echo ============================================
echo   Tide Monitor - Starting Development Servers
echo ============================================
echo.

:: Start the backend .NET API
echo [1/3] Starting backend API on http://localhost:5000...
start "TideMonitor-Backend" cmd /c "cd /d "%~dp0backend\TideMonitor.Api" && dotnet run"

:: Wait a moment for the backend to start initializing
timeout /t 5 /nobreak >nul

:: Start the frontend Vite dev server
echo [2/3] Starting frontend on http://localhost:5173...
start "TideMonitor-Frontend" cmd /c "cd /d "%~dp0frontend\tide-dashboard" && npm run dev"

:: Wait for the frontend to start
timeout /t 3 /nobreak >nul

:: Open the browser
echo [3/3] Opening browser...
start http://localhost:5173

echo.
echo ============================================
echo   Servers are starting up!
echo   Backend:  http://localhost:5000
echo   Frontend: http://localhost:5173
echo ============================================
echo.
echo Close this window to stop monitoring.
pause
