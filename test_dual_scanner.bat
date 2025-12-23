@echo off
echo ============================================
echo Testing Dual Scanner Initialization
echo ============================================
echo.

cd PrintMate.Terminal\bin\Debug\net9.0-windows

echo Starting main application...
echo Watch for messages from both scanners:
echo   - "Scanner 0" should connect successfully
echo   - "Scanner 1" should connect successfully
echo.
echo Press Ctrl+C to stop
echo ============================================
echo.

PrintMate.Terminal.exe

pause
