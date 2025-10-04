# Combined build and deploy script for Aukett + Heese Revit Add-in
# Builds the project and immediately deploys it to Revit 2024

Write-Host "======================================" -ForegroundColor Magenta
Write-Host "Aukett + Heese Add-in Builder & Deployer" -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor Magenta
Write-Host ""

# Step 1: Build
Write-Host "STEP 1: Building project..." -ForegroundColor Cyan
& .\build.ps1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Deployment aborted."
    exit 1
}

Write-Host ""

# Step 2: Deploy  
Write-Host "STEP 2: Deploying to Revit..." -ForegroundColor Cyan
& .\deploy.ps1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed."
    exit 1
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "BUILD AND DEPLOY COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Your add-in is ready to test in Revit 2024!" -ForegroundColor Yellow