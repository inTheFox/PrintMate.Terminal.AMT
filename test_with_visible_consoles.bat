@echo off
echo ============================================
echo Testing Hans Scanner Hosts with Visible Consoles
echo ============================================
echo.
echo This will start the main application.
echo Host consoles should appear automatically.
echo.
echo What to look for:
echo   1. Two console windows should open (for each scanner)
echo   2. Each console should show:
echo      - "Pipe Name: hans_scanner_X"
echo      - "IP Address: 172.18.34.22X"
echo      - "READY - waiting for client connections..."
echo   3. Main app should connect to both
echo.
echo If you DON'T see host console windows:
echo   - Check that HansScannerHost.exe exists in bin folder
echo   - Check that CreateNoWindow = false in ScanatorProxyClient
echo.
echo Press any key to start...
pause > nul
echo.
echo Starting main application...
echo ============================================
echo.

dotnet run --project PrintMate.Terminal\PrintMate.Terminal.csproj --configuration Debug

pause
