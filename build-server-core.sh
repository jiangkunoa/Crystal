#!/bin/bash

# Cross-platform build script for Server.Core

echo "Building cross-platform Server.Core..."

# Build the project
dotnet build Server.Core/Server.Core.csproj

if [ $? -eq 0 ]; then
    echo "Build successful!"
    echo "To run the server, use: Build/Server.Core/Debug/Server.Core.exe"
    echo "Or use: dotnet Build/Server.Core/Debug/Server.Core.dll"
else
    echo "Build failed!"
    exit 1
fi