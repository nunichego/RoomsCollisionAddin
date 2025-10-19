# deploy.ps1 - Deployment automation for RoomsManagerAddin
# Version: 2.0
# Last Updated: 2025-10-19

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [Parameter(Mandatory=$false)]
    [switch]$SkipBackup = $false,

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false
)

Write-Host "=== RoomsManagerAddin Deployment Script v2.0 ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Step 1: Build the project
if (-not $SkipBuild) {
    Write-Host "Step 1: Building project..." -ForegroundColor Yellow
    try {
        dotnet clean RoomsManagerAddin.csproj --configuration $Configuration --verbosity quiet | Out-Null
        dotnet build RoomsManagerAddin.csproj --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            Write-Host "- Build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "- Build successful" -ForegroundColor Green
    }
    catch {
        Write-Host "- Build failed with error: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Step 1: Build skipped (using existing binaries)" -ForegroundColor Yellow
}

Write-Host ""

# Step 2: Check if built files exist
$dllPath = "bin\$Configuration\net48\RoomsManagerAddin.dll"
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

# Step 2.5: Backup current deployment
if (-not $SkipBackup) {
    $backupBaseDir = "C:\Backups\RoomsManagerAddin"
    $backupDir = "$backupBaseDir\backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    $revitAddinsPath = "$env:APPDATA\Autodesk\Revit\Addins\2024"

    if (Test-Path "$revitAddinsPath\RoomsManagerAddin.dll") {
        Write-Host "Step 2.5: Creating backup..." -ForegroundColor Yellow

        if (-not (Test-Path $backupBaseDir)) {
            New-Item -ItemType Directory -Path $backupBaseDir -Force | Out-Null
        }

        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        Copy-Item "$revitAddinsPath\RoomsManagerAddin.dll" $backupDir -ErrorAction SilentlyContinue
        Copy-Item "$revitAddinsPath\RoomsManagerAddin.addin" $backupDir -ErrorAction SilentlyContinue

        Write-Host "- Backup created: $backupDir" -ForegroundColor Green
    } else {
        Write-Host "Step 2.5: No existing deployment to backup" -ForegroundColor Yellow
    }
    Write-Host ""
}

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
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Restart Revit 2024" -ForegroundColor White
Write-Host "  2. Verify 'AH RoomsDataSync (Demo)' panel appears in ribbon" -ForegroundColor White
Write-Host "  3. Run smoke test:" -ForegroundColor White
Write-Host "     - Open test model with rooms" -ForegroundColor Gray
Write-Host "     - Click 'RoomsMapping' button" -ForegroundColor Gray
Write-Host "     - Load rooms and walls/floors" -ForegroundColor Gray
Write-Host "     - Run basic analysis" -ForegroundColor Gray
Write-Host "     - Verify log file is created" -ForegroundColor Gray
Write-Host ""
Write-Host "Usage Examples:" -ForegroundColor Cyan
Write-Host "  .\deploy.ps1                          # Deploy Debug build with backup" -ForegroundColor Gray
Write-Host "  .\deploy.ps1 -Configuration Release   # Deploy Release build" -ForegroundColor Gray
Write-Host "  .\deploy.ps1 -SkipBackup              # Skip backup step" -ForegroundColor Gray
Write-Host "  .\deploy.ps1 -SkipBuild               # Skip build, deploy existing binaries" -ForegroundColor Gray
Write-Host ""