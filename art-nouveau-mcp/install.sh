#!/bin/bash

echo "🎨 Installing Art Nouveau MCP Server..."
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null
then
    echo "❌ Node.js is not installed."
    echo "   Download from: https://nodejs.org/"
    exit 1
fi

NODE_VERSION=$(node --version)
echo "✓ Node.js version: $NODE_VERSION"

# Install dependencies
echo ""
echo "📦 Installing dependencies..."
npm install

if [ $? -ne 0 ]; then
    echo "❌ npm install failed"
    exit 1
fi

# Build the server
echo ""
echo "🔨 Building TypeScript..."
npm run build

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo ""
echo "✅ Installation complete!"
echo ""
echo "To run the server:"
echo "  npm start"
echo ""
echo "To add to Claude Desktop:"
CURRENT_DIR=$(pwd)
echo '  "art-nouveau-anchoring": {'
echo '    "command": "node",'
echo "    "args": ["$CURRENT_DIR/build/index.js"]"
echo '  }'
