#!/bin/bash
set -e

echo "🏗️  Building frontend..."
cd "$(dirname "$0")/../frontend"

# Clean previous builds
rm -rf dist

# Install dependencies if node_modules doesn't exist
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
fi

# Build Angular app
echo "🔨 Building Angular application..."
npm run build

echo "✅ Frontend build complete: dist/"
