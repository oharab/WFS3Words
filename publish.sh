#!/bin/bash
# Publish script for WFS3Words - prepares for IIS deployment

set -e

OUTPUT_DIR="./publish"

echo "Publishing WFS3Words.Api for IIS deployment..."
dotnet publish src/WFS3Words.Api/WFS3Words.Api.csproj \
  --configuration Release \
  --output "$OUTPUT_DIR" \
  --self-contained false

echo ""
echo "Publish completed successfully!"
echo "Output directory: $OUTPUT_DIR"
echo ""
echo "Next steps for IIS deployment:"
echo "1. Copy the contents of '$OUTPUT_DIR' to your IIS server"
echo "2. Follow the instructions in DEPLOYMENT.md"
