@echo off
echo Starting HansScannerHost manually for testing...
cd HansScannerHost\bin\Debug\net9.0-windows
HansScannerHost.exe test_pipe_0 172.18.34.227 0
pause
