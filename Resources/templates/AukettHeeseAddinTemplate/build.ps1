# Build script for Aukett + Heese Revit Add-in
# Builds the project using dotnet build command

Write-Host "Building Aukett + Heese Revit Add-in..." -ForegroundColor Green

try {
    # Get the project file name (assuming only one .csproj file)
    $projectFile = Get-ChildItem "*.csproj" | Select-Object -First 1
    
    if (-not $projectFile) {
        Write-Error "No .csproj file found in current directory"
        exit 1
    }

    Write-Host "Project file: $($projectFile.Name)" -ForegroundColor Yellow
    
    # Build the project
    $buildResult = dotnet build $projectFile.Name --configuration Debug --verbosity minimal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build completed successfully!" -ForegroundColor Green
        
        # Show the output DLL location
        $dllPath = "bin\Debug\net48\$($projectFile.BaseName).dll"
        if (Test-Path $dllPath) {
            Write-Host "Output DLL: $dllPath" -ForegroundColor Cyan
            $dllInfo = Get-Item $dllPath
            Write-Host "DLL size: $([math]::Round($dllInfo.Length / 1KB, 2)) KB" -ForegroundColor Cyan
            Write-Host "Last modified: $($dllInfo.LastWriteTime)" -ForegroundColor Cyan
        }
    } else {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit 1
    }
} catch {
    Write-Error "Build script error: $($_.Exception.Message)"
    exit 1
}