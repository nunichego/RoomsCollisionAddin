# Deploy script for Aukett + Heese Revit Add-in
# Deploys the built DLL and .addin file to Revit 2024

Write-Host "Deploying Aukett + Heese Revit Add-in..." -ForegroundColor Green

try {
    # Get the project file name (assuming only one .csproj file)
    $projectFile = Get-ChildItem "*.csproj" | Select-Object -First 1
    $addinFile = Get-ChildItem "*.addin" | Select-Object -First 1
    
    if (-not $projectFile) {
        Write-Error "No .csproj file found in current directory"
        exit 1
    }
    
    if (-not $addinFile) {
        Write-Error "No .addin file found in current directory"
        exit 1
    }

    # Define paths
    $dllPath = "bin\Debug\net48\$($projectFile.BaseName).dll"
    $revitAddinsPath = "$env:APPDATA\Autodesk\Revit\Addins\2024"
    
    # Check if DLL exists
    if (-not (Test-Path $dllPath)) {
        Write-Error "DLL not found at $dllPath. Please build the project first using build.ps1"
        exit 1
    }

    # Ensure Revit addins directory exists
    if (-not (Test-Path $revitAddinsPath)) {
        Write-Host "Creating Revit addins directory: $revitAddinsPath" -ForegroundColor Yellow
        New-Item -Path $revitAddinsPath -ItemType Directory -Force | Out-Null
    }

    # Copy DLL
    Write-Host "Copying DLL: $dllPath -> $revitAddinsPath" -ForegroundColor Yellow
    Copy-Item $dllPath $revitAddinsPath -Force

    # Copy .addin manifest
    Write-Host "Copying .addin file: $($addinFile.Name) -> $revitAddinsPath" -ForegroundColor Yellow
    Copy-Item $addinFile.Name $revitAddinsPath -Force

    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    
    # Verify deployment
    $deployedDll = Join-Path $revitAddinsPath $projectFile.BaseName + ".dll"
    $deployedAddin = Join-Path $revitAddinsPath $addinFile.Name
    
    if ((Test-Path $deployedDll) -and (Test-Path $deployedAddin)) {
        Write-Host "Verification: Files successfully deployed" -ForegroundColor Green
        
        $dllInfo = Get-Item $deployedDll
        Write-Host "DLL timestamp: $($dllInfo.LastWriteTime)" -ForegroundColor Cyan
        
        Write-Host "`nNext steps:" -ForegroundColor Yellow
        Write-Host "1. Restart Revit 2024 completely" -ForegroundColor White
        Write-Host "2. Look for the 'Aukett + Heese' tab in the ribbon" -ForegroundColor White
        Write-Host "3. Click your 'Hello World' button to test" -ForegroundColor White
    } else {
        Write-Error "Verification failed: Files not found in deployment location"
        exit 1
    }
    
} catch {
    Write-Error "Deploy script error: $($_.Exception.Message)"
    exit 1
}