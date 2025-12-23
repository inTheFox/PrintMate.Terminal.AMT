@echo off
echo ================================================
echo Testing Single HansScannerHost Process
echo ================================================
echo.
echo This will start ONE host process manually.
echo Watch the console output carefully.
echo.
echo Expected output:
echo   1. "Waiting for client connection..."
echo   2. "Client connected"
echo   3. "Waiting for client to setup streams (500ms)..."
echo   4. "Ready to handle commands"
echo   5. "Received: {...Ping...}"
echo   6. "Sent: {...Success:true...}"
echo.
echo Press Ctrl+C to stop
echo ================================================
echo.

cd HansScannerHost\bin\Debug\net9.0-windows
HansScannerHost.exe test_single_pipe 172.18.34.227 0

pause
