#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
LAMBDA_DIR="$PROJECT_ROOT/infrastructure/lambda"
TERRAFORM_DIR="$PROJECT_ROOT/infrastructure/terraform"

echo "🔨 Building Lambda handler..."
cd "$LAMBDA_DIR"

# Clean previous builds
rm -rf bin obj handler.zip

# Restore and build
echo "📦 Restoring dependencies..."
dotnet restore -r linux-x64

echo "🔨 Building handler..."
dotnet build -c Release

# Publish as self-contained for Linux x86_64 (Lambda runtime)
echo "📦 Publishing handler..."
dotnet publish -c Release -o publish --self-contained true --no-restore -r linux-x64

# Create deployment package
echo "📦 Creating deployment package..."
cd publish
zip -r ../handler.zip . -x "*.pdb"
cd ..

echo "✅ Lambda handler packaged: handler.zip"
echo "📍 Size: $(ls -lh handler.zip | awk '{print $5}')"
