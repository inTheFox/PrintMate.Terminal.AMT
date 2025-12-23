@echo off
echo Stopping all running processes...

echo Stopping PrintMate.Terminal...
taskkill /F /IM "PrintMate.Terminal.exe" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   - PrintMate.Terminal stopped
) else (
    echo   - PrintMate.Terminal not running
)

echo Stopping HansScannerHost...
taskkill /F /IM "HansScannerHost.exe" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   - HansScannerHost stopped
) else (
    echo   - HansScannerHost not running
)

echo.
echo All processes stopped. You can now rebuild.
pause
