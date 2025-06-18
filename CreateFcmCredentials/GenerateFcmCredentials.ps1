# FCM Credentials Generator for Rust+ Home Assistant Bridge
# This script uses @liamcottle/rustplus.js to generate FCM credentials

Write-Host "Installing @liamcottle/rustplus.js..." -ForegroundColor Green
npm install @liamcottle/rustplus.js

Write-Host "`nGenerating FCM credentials..." -ForegroundColor Green
Write-Host "Follow the prompts to register with FCM..." -ForegroundColor Yellow
npx @liamcottle/rustplus.js fcm-register

Write-Host "`nFCM registration complete!" -ForegroundColor Green
Write-Host "Look for the generated .json file in the current directory." -ForegroundColor Yellow
Write-Host "You can use this file with the Rust+ Home Assistant Bridge FCM service." -ForegroundColor Cyan
