@echo off
REM Windows build script for Server.Core

echo Building cross-platform Server.Core...

REM Build the project
dotnet build Server.Core/Server.Core.csproj

if %errorlevel% == 0 (
    echo Build successful!
    echo To run the server, use: Build\Server.Core\Debug\Server.Core.exe
    echo Or use: dotnet Build\Server.Core\Debug\Server.Core.dll
) else (
    echo Build failed!
    exit /b 1
)