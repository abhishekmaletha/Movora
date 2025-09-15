@echo off

echo 🎬 Starting Movora UI...

:: Check if Node.js is installed
node --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Node.js is not installed. Please install Node.js 16+ first.
    pause
    exit /b 1
)

:: Check if dependencies are installed
if not exist "node_modules" (
    echo 📦 Installing dependencies...
    npm install
)

:: Check if .env exists
if not exist ".env" (
    echo ⚙️ Creating .env file...
    copy .env.example .env
    echo ✅ Created .env file. Update REACT_APP_API_BASE_URL if needed.
)

echo 🚀 Starting development server...
npm start
