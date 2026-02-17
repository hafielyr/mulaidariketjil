@echo off
echo 🎨 Installing Art Nouveau MCP Server...
echo.

REM Check if Node.js is installed
where node >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Node.js is not installed.
    echo    Download from: https://nodejs.org/
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('node --version') do set NODE_VERSION=%%i
echo ✓ Node.js version: %NODE_VERSION%

REM Install dependencies
echo.
echo 📦 Installing dependencies...
call npm install

if %ERRORLEVEL% NEQ 0 (
    echo ❌ npm install failed
    pause
    exit /b 1
)

REM Build the server
echo.
echo 🔨 Building TypeScript...
call npm run build

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed
    pause
    exit /b 1
)

echo.
echo ✅ Installation complete!
echo.
echo To run the server:
echo   npm start
echo.
echo To add to Claude Desktop config:
echo   "art-nouveau-anchoring": {
echo     "command": "node",
echo     "args": ["%CD%\build\index.js"]
echo   }
echo.
pause
