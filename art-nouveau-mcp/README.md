# Art Nouveau Visual Anchoring MCP Server (Node.js)

An MCP (Model Context Protocol) server built with **Node.js/TypeScript** that provides AI agents with reference images and style information about Alphonse Mucha's work and Art Nouveau architecture.

> **Python-free alternative** - Perfect for organizations that don't allow Python!
> **Fixed for Windows** - Type errors resolved, installation simplified!

## 🚀 Quick Start

### Prerequisites
- Node.js 18 or higher ([Download](https://nodejs.org/))
- npm (comes with Node.js)

### Installation

#### Option 1: Automated (Recommended)

**Windows:**
```cmd
install.bat
```

**macOS/Linux:**
```bash
chmod +x install.sh
./install.sh
```

#### Option 2: Manual

```bash
# 1. Install dependencies
npm install

# 2. Build TypeScript
npm run build

# 3. Test it works
npm start
```

## 🔧 Integration with Claude Desktop

### Find your config file:
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Linux**: `~/.config/Claude/claude_desktop_config.json`

### Add to config:

```json
{
  "mcpServers": {
    "art-nouveau-anchoring": {
      "command": "node",
      "args": ["C:\\Full\\Path\\To\\build\\index.js"]
    }
  }
}
```

⚠️ **Important for Windows**: 
- Use FULL absolute path (e.g., `C:\Users\YourName\art-nouveau-mcp\build\index.js`)
- Use double backslashes `\\` in JSON
- Or use forward slashes: `C:/Users/YourName/art-nouveau-mcp/build/index.js`

### Restart Claude Desktop

Close Claude Desktop completely and restart.

## 🎨 Available Tools

### 1. get_mucha_references
Get reference images of Alphonse Mucha's Art Nouveau artwork (1-10 images).

### 2. get_architecture_references
Get Art Nouveau architecture reference images (1-10 images).

### 3. get_style_characteristics
Get detailed Art Nouveau style information with citations.

### 4. get_combined_anchors
Get both artwork and architecture references together.

### 5. create_prompt_with_anchors
Generate enhanced prompts with Art Nouveau visual anchors.

## 📊 What's Included

- **10 Alphonse Mucha artworks** with high-resolution URLs
- **10 Art Nouveau architecture** examples
- **Comprehensive style guide** with characteristics and citations

## 🐛 Troubleshooting

### Build errors on Windows
If you get TypeScript errors, make sure you have the latest Node.js:
```cmd
node --version
```
Should be v18.0.0 or higher.

### "Cannot find module" error
```bash
# Delete and reinstall
rm -rf node_modules
npm install
npm run build
```

### Permission denied (macOS/Linux)
```bash
chmod +x build/index.js
chmod +x install.sh
```

### Claude Desktop can't find the server
1. Check the path in config is absolute (full path)
2. Run `npm start` manually to test
3. Check Claude Desktop logs for errors

## 📝 Testing

Test the server manually:
```bash
npm start
```

You should see: `Art Nouveau MCP Server running on stdio`

Press Ctrl+C to stop.

## 🏗️ Project Structure

```
art-nouveau-anchoring-mcp/
├── index.ts              # Main server (TypeScript source)
├── package.json          # Dependencies and scripts
├── tsconfig.json         # TypeScript configuration
├── build/                # Compiled JavaScript (after build)
│   └── index.js         # Executable
├── install.sh           # Unix installer
├── install.bat          # Windows installer
└── README.md            # This file
```

## 🔄 Development

Run in development mode with auto-reload:
```bash
npm run dev
```

## 📝 License

MIT License - Free to use and modify

## 🤝 Contributing

Suggestions and improvements welcome!
