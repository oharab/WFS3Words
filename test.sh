#!/bin/bash
# Test script for WFS3Words

set -e

echo "Running unit tests..."
dotnet test tests/WFS3Words.Tests.Unit/WFS3Words.Tests.Unit.csproj --configuration Release --logger "console;verbosity=detailed"

echo ""
echo "Running integration tests..."
dotnet test tests/WFS3Words.Tests.Integration/WFS3Words.Tests.Integration.csproj --configuration Release --logger "console;verbosity=detailed"

echo ""
echo "All tests completed successfully!"
