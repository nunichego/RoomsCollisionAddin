# Revit Add-in Build and Deploy Script
# This script builds the RoomsManagerAddin project and deploys it to Revit 2024

Write-Host "=== Revit Add-in Build and Deploy Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the project
Write-Host "Step 1: Building project..." -ForegroundColor Yellow
try {
    dotnet build RoomsManagerAddin.csproj --configuration Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "- Build successful" -ForegroundColor Green
}
catch {
    Write-Host "Build failed with error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Check if built files exist
$dllPath = "bin\Debug\net48\RoomsManagerAddin.dll"
$addinPath = "RoomsManagerAddin.addin"

Write-Host "Step 2: Verifying build outputs..." -ForegroundColor Yellow

if (!(Test-Path $dllPath)) {
    Write-Host "- DLL not found at: $dllPath" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $addinPath)) {
    Write-Host "- Addin manifest not found at: $addinPath" -ForegroundColor Red
    exit 1
}

# Get DLL info
$dllInfo = Get-Item $dllPath
$dllSize = [math]::Round($dllInfo.Length / 1KB, 1)
Write-Host "- DLL found: $($dllInfo.Name) ($dllSize KB, modified: $($dllInfo.LastWriteTime))" -ForegroundColor Green

# Get addin manifest info
$addinInfo = Get-Item $addinPath
Write-Host "- Addin manifest found: $($addinInfo.Name) (modified: $($addinInfo.LastWriteTime))" -ForegroundColor Green

Write-Host ""

# Step 3: Deploy to Revit addins folder
$revitAddinsPath = "$env:APPDATA\Autodesk\Revit\Addins\2024"
Write-Host "Step 3: Deploying to Revit 2024..." -ForegroundColor Yellow

# Ensure target directory exists
if (!(Test-Path $revitAddinsPath)) {
    Write-Host "Creating Revit addins directory: $revitAddinsPath" -ForegroundColor Cyan
    New-Item -Path $revitAddinsPath -ItemType Directory -Force | Out-Null
}

try {
    # Copy DLL
    Copy-Item $dllPath $revitAddinsPath -Force
    Write-Host "- DLL deployed successfully" -ForegroundColor Green
    
    # Copy addin manifest
    Copy-Item $addinPath $revitAddinsPath -Force
    Write-Host "- Addin manifest deployed successfully" -ForegroundColor Green
}
catch {
    Write-Host "- Deployment failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Verify deployment
Write-Host "Step 4: Verifying deployment..." -ForegroundColor Yellow

$deployedFiles = Get-ChildItem $revitAddinsPath | Where-Object Name -like "*RoomsManager*"

if ($deployedFiles.Count -eq 0) {
    Write-Host "- No deployed files found!" -ForegroundColor Red
    exit 1
}

Write-Host "- Deployed files in ${revitAddinsPath}:" -ForegroundColor Green
foreach ($file in $deployedFiles) {
    $size = if ($file.Length) { [math]::Round($file.Length / 1KB, 1) } else { "N/A" }
    Write-Host "  - $($file.Name) ($size KB, $($file.LastWriteTime))" -ForegroundColor White
}

Write-Host ""
Write-Host "=== DEPLOYMENT COMPLETE ===" -ForegroundColor Green
Write-Host "The add-in has been successfully built and deployed to Revit 2024." -ForegroundColor White
Write-Host "Restart Revit to load the updated add-in." -ForegroundColor Cyan
Write-Host ""