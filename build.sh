#!/bin/bash
# Build script for WFS3Words

set -e

echo "Building WFS3Words solution..."
dotnet build WFS3Words.sln --configuration Release

echo "Build completed successfully!"
