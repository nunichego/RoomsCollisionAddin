# Deployment Testing Guide

**Version**: 2.0 (Post-Refactoring)
**Last Updated**: 2025-10-19

## Overview

This guide covers the deployment process and verification steps for RoomsManagerAddin.

---

## Build Process

### 1. Clean Build

```bash
# Clean previous builds
dotnet clean RoomsManagerAddin.csproj --configuration Debug
dotnet clean RoomsManagerAddin.csproj --configuration Release

# Build Debug version
dotnet build RoomsManagerAddin.csproj --configuration Debug

# Build Release version
dotnet build RoomsManagerAddin.csproj --configuration Release
```

**Expected Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:XX.XX
```

### 2. Verify Build Output

**Check DLL location**:
```powershell
Get-ChildItem 'bin\Debug\net48\RoomsManagerAddin.dll'
```

**Verify DLL timestamp**:
```powershell
(Get-Item 'bin\Debug\net48\RoomsManagerAddin.dll').LastWriteTime
```

**Timestamp should match current build time** (within 1-2 minutes).

---

## Deployment Process

### Standard Deployment (Debug)

```powershell
# 1. Build project
dotnet build RoomsManagerAddin.csproj --configuration Debug

# 2. Deploy DLL
powershell -Command "Copy-Item 'bin\\Debug\\net48\\RoomsManagerAddin.dll' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"

# 3. Deploy manifest
powershell -Command "Copy-Item 'RoomsManagerAddin.addin' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"

# 4. Verify deployment
powershell -Command "Get-ChildItem 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\' | Where-Object Name -like '*RoomsManager*'"
```

### Production Deployment (Release)

```powershell
# 1. Build Release version
dotnet build RoomsManagerAddin.csproj --configuration Release

# 2. Deploy Release DLL
powershell -Command "Copy-Item 'bin\\Release\\net48\\RoomsManagerAddin.dll' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"

# 3. Deploy manifest
powershell -Command "Copy-Item 'RoomsManagerAddin.addin' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"
```

---

## Deployment Verification

### 1. File System Verification

**Check DLL exists**:
```powershell
Test-Path 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RoomsManagerAddin.dll'
# Should return: True
```

**Check manifest exists**:
```powershell
Test-Path 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RoomsManagerAddin.addin'
# Should return: True
```

**Verify DLL timestamp**:
```powershell
(Get-Item 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RoomsManagerAddin.dll').LastWriteTime
```

**Timestamp should match build time** - if not, old version is deployed!

### 2. Revit Loading Verification

**Steps**:
1. Close Revit (if open)
2. Open Revit 2024
3. Look for "AH RoomsDataSync (Demo)" panel in ribbon

**Verify**:
- [ ] Ribbon panel appears
- [ ] Three buttons visible: "RoomsMapping", "Settings", "Help"
- [ ] Button icons display correctly (not placeholder icons)

### 3. Functional Verification (Smoke Test)

**Quick Smoke Test** (5 minutes):
1. Open test Revit model with rooms
2. Click "RoomsMapping" button
3. Window opens without errors
4. Click "Load Rooms and Walls"
5. Rooms list populates
6. Walls list populates
7. Select 1-2 rooms and walls
8. Click "Run Analysis"
9. Analysis completes without crash
10. Log file is created

**Pass Criteria**:
- ✅ No crashes
- ✅ No error dialogs
- ✅ Log file contains analysis details

---

## Common Deployment Issues

### Issue 1: Old Version Still Loading

**Symptoms**:
- Changes not reflected in Revit
- Old bugs still present
- Deployed DLL timestamp matches build, but behavior is old

**Causes**:
- Revit cached the old DLL
- DLL locked by Revit process
- Multiple copies of DLL in different locations

**Solutions**:

**Solution A: Complete Revit Restart**
```powershell
# 1. Close Revit
# 2. Kill any lingering Revit processes
Stop-Process -Name "Revit" -Force -ErrorAction SilentlyContinue

# 3. Wait 5 seconds
Start-Sleep -Seconds 5

# 4. Re-deploy
powershell -Command "Copy-Item 'bin\\Debug\\net48\\RoomsManagerAddin.dll' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\' -Force"

# 5. Start Revit fresh
```

**Solution B: Clear Revit Add-in Cache**
```powershell
# Delete Revit add-in cache (location varies by Revit version)
Remove-Item 'C:\Users\$env:USERNAME\AppData\Local\Autodesk\Revit\*\CollaborationCache\*' -Recurse -Force -ErrorAction SilentlyContinue
```

### Issue 2: DLL Not Found

**Symptoms**:
- Add-in doesn't appear in Revit ribbon
- Revit shows "Add-in failed to load" warning

**Check**:
1. **Verify manifest file path**:
   ```xml
   <!-- RoomsManagerAddin.addin -->
   <Assembly>RoomsManagerAddin.dll</Assembly>
   ```
   Should be just filename, not full path.

2. **Verify DLL and manifest in same directory**:
   ```powershell
   Get-ChildItem 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RoomsManager*'
   ```
   Should show both `.dll` and `.addin` files.

3. **Check .addin file ClientId**:
   ```xml
   <AddIn Type="Command">
     <Assembly>RoomsManagerAddin.dll</Assembly>
     <AddInId>A4C2C010-C134-4D60-9E42-50F527A3F7A1</AddInId>
     <!-- ... -->
   </AddIn>
   ```
   ClientId should match the GUID in `App.cs`.

### Issue 3: "Cannot find path bin\Debug\RoomsManagerAddin.dll"

**Cause**: Copying from wrong directory

**Fix**: Always use `bin\Debug\net48\RoomsManagerAddin.dll`, not `bin\Debug\RoomsManagerAddin.dll`

**Correct Path**:
```powershell
# CORRECT
Copy-Item 'bin\\Debug\\net48\\RoomsManagerAddin.dll' ...

# INCORRECT
Copy-Item 'bin\\Debug\\RoomsManagerAddin.dll' ...
```

### Issue 4: "Access Denied" when copying DLL

**Cause**: DLL locked by Revit or another process

**Fix**:
```powershell
# 1. Close Revit completely
# 2. Kill Revit processes
Stop-Process -Name "Revit" -Force -ErrorAction SilentlyContinue

# 3. Wait 5 seconds
Start-Sleep -Seconds 5

# 4. Try deployment again
Copy-Item 'bin\\Debug\\net48\\RoomsManagerAddin.dll' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\' -Force
```

### Issue 5: Dependencies Missing

**Symptoms**:
- Add-in loads but crashes on execution
- Error: "Could not load file or assembly ..."

**Check Dependencies**:
```powershell
# Verify RevitAPI.dll and RevitAPIUI.dll are NOT copied to addins folder
# They should be referenced from Revit installation directory
Get-ChildItem 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RevitAPI*.dll'
# Should return EMPTY (no RevitAPI*.dll files)
```

**If RevitAPI.dll is copied**:
```powershell
# Delete them (they should be referenced from Revit installation, not copied)
Remove-Item 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RevitAPI*.dll' -Force
```

---

## Multi-User Deployment

### Network Share Deployment

**Setup**:
1. Create network share: `\\server\RoomsManagerAddin\`
2. Copy DLL and manifest to share
3. Update .addin file paths on each user machine:

**Modified .addin file**:
```xml
<AddIn Type="Command">
  <Assembly>\\server\RoomsManagerAddin\RoomsManagerAddin.dll</Assembly>
  <!-- ... -->
</AddIn>
```

**Pros**:
- Single deployment location
- Easy to update for all users

**Cons**:
- Network latency on load
- Requires network share permissions

### Local Deployment Script

**For IT departments deploying to multiple machines**:

```powershell
# deploy-to-all-users.ps1

$users = @("user1", "user2", "user3")
$sourceDLL = "\\build-server\builds\RoomsManagerAddin\RoomsManagerAddin.dll"
$sourceManifest = "\\build-server\builds\RoomsManagerAddin\RoomsManagerAddin.addin"

foreach ($user in $users) {
    $targetDir = "C:\Users\$user\AppData\Roaming\Autodesk\Revit\Addins\2024\"

    if (Test-Path $targetDir) {
        Copy-Item $sourceDLL $targetDir -Force
        Copy-Item $sourceManifest $targetDir -Force
        Write-Output "Deployed to $user"
    } else {
        Write-Warning "Target directory not found for $user"
    }
}
```

---

## Version Management

### Version Tracking

**In Code** (`AssemblyInfo.cs`):
```csharp
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]
```

**In Manifest** (`RoomsManagerAddin.addin`):
```xml
<!-- Add version comment -->
<!-- Version: 2.0 -->
<!-- Build Date: 2025-10-19 -->
```

### Deployment Naming Convention

For version tracking, use timestamped folders:

```
C:\Deployments\
  RoomsManagerAddin_v2.0_20251019\
    RoomsManagerAddin.dll
    RoomsManagerAddin.addin
```

---

## Rollback Procedure

If deployment fails or introduces issues:

### Quick Rollback

```powershell
# 1. Copy previous version from backup
Copy-Item 'C:\Backups\RoomsManagerAddin_v1.1\RoomsManagerAddin.dll' 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\' -Force

# 2. Copy previous manifest
Copy-Item 'C:\Backups\RoomsManagerAddin_v1.1\RoomsManagerAddin.addin' 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\' -Force

# 3. Restart Revit
```

### Best Practice: Always Backup Before Deployment

```powershell
# Before deploying new version, backup current
$backupDir = "C:\Backups\RoomsManagerAddin_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $backupDir -Force

Copy-Item 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RoomsManagerAddin.dll' $backupDir
Copy-Item 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\RoomsManagerAddin.addin' $backupDir

Write-Output "Backup created at: $backupDir"
```

---

## Deployment Checklist

### Pre-Deployment

- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] Git commit all changes
- [ ] Tag version in git: `git tag v2.0`
- [ ] Backup current deployment
- [ ] Close Revit on target machine

### Deployment

- [ ] Deploy DLL to Revit addins folder
- [ ] Deploy .addin manifest
- [ ] Verify DLL timestamp matches build
- [ ] Verify .addin file is valid XML

### Post-Deployment

- [ ] Open Revit
- [ ] Verify ribbon panel appears
- [ ] Run smoke test (load rooms, run analysis)
- [ ] Check log file for errors
- [ ] Test critical features:
  - [ ] Room-Wall analysis
  - [ ] Room-Floor analysis
  - [ ] Filtering
  - [ ] Parameter mapping

### Rollback (if needed)

- [ ] Restore backup DLL
- [ ] Restore backup .addin
- [ ] Restart Revit
- [ ] Verify previous version works
- [ ] Investigate deployment issue

---

## Deployment to Production

### Production Checklist

- [ ] All manual tests passed (see `manual-testing-checklist.md`)
- [ ] Performance benchmarks met
- [ ] No critical bugs open
- [ ] Documentation updated
- [ ] Release notes prepared
- [ ] User communication sent (if applicable)

### Release Package

**Contents**:
```
RoomsManagerAddin_v2.0_Release.zip
  ├── RoomsManagerAddin.dll
  ├── RoomsManagerAddin.addin
  ├── README.txt (installation instructions)
  ├── CHANGELOG.txt (version history)
  └── LICENSE.txt (if applicable)
```

**README.txt template**:
```
RoomsManagerAddin v2.0 - Installation Instructions

1. Close Revit 2024 if open

2. Copy RoomsManagerAddin.dll and RoomsManagerAddin.addin to:
   C:\Users\<YourUsername>\AppData\Roaming\Autodesk\Revit\Addins\2024\

3. Restart Revit 2024

4. Look for "AH RoomsDataSync (Demo)" panel in ribbon

For support, contact: [your email/support channel]
```

---

## Troubleshooting Deployment

### Enable Revit Add-in Logging

Revit logs add-in errors to:
```
C:\Users\<Username>\AppData\Local\Autodesk\Revit\Autodesk Revit 2024\Journals\journal.XXXXXXXX.txt
```

**Check for errors**:
```powershell
# Find latest journal file
Get-ChildItem 'C:\Users\$env:USERNAME\AppData\Local\Autodesk\Revit\Autodesk Revit 2024\Journals\' | Sort-Object LastWriteTime -Descending | Select-Object -First 1

# Search for add-in errors
Get-Content '<journal-file-path>' | Select-String -Pattern "RoomsManager|Error|Exception"
```

### Common Journal File Errors

**Error: "Could not load assembly"**
- Check DLL is in correct location
- Verify .addin file Assembly path is correct
- Ensure Revit API references are NOT copied to addins folder

**Error: "Method not found"**
- DLL/Revit version mismatch
- Rebuild against correct Revit API version

**Error: "Type initializer exception"**
- Error in static constructor (e.g., `App.OnStartup`)
- Check GlobalErrorHandler initialization
- Check DI container configuration

---

## Appendix: Deployment Scripts

### Complete Deployment Script

```powershell
# deploy.ps1 - Complete deployment automation

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Write-Output "=== RoomsManagerAddin Deployment Script ==="
Write-Output "Configuration: $Configuration"
Write-Output ""

# 1. Build
Write-Output "Building project..."
dotnet clean RoomsManagerAddin.csproj --configuration $Configuration
dotnet build RoomsManagerAddin.csproj --configuration $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# 2. Backup current deployment
$backupDir = "C:\Backups\RoomsManagerAddin_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Write-Output "Creating backup at: $backupDir"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

$targetDir = "C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\"
if (Test-Path "$targetDir\RoomsManagerAddin.dll") {
    Copy-Item "$targetDir\RoomsManagerAddin.dll" $backupDir
    Copy-Item "$targetDir\RoomsManagerAddin.addin" $backupDir
    Write-Output "Backup created successfully"
}

# 3. Deploy new version
Write-Output "Deploying new version..."
Copy-Item "bin\$Configuration\net48\RoomsManagerAddin.dll" $targetDir -Force
Copy-Item "RoomsManagerAddin.addin" $targetDir -Force

# 4. Verify deployment
$dllTimestamp = (Get-Item "$targetDir\RoomsManagerAddin.dll").LastWriteTime
Write-Output "Deployed DLL timestamp: $dllTimestamp"

# 5. Verification
Write-Output ""
Write-Output "=== Deployment Summary ==="
Write-Output "DLL deployed to: $targetDir\RoomsManagerAddin.dll"
Write-Output "Manifest deployed to: $targetDir\RoomsManagerAddin.addin"
Write-Output "Backup location: $backupDir"
Write-Output ""
Write-Output "Next steps:"
Write-Output "1. Restart Revit 2024"
Write-Output "2. Verify ribbon panel appears"
Write-Output "3. Run smoke test"
Write-Output ""
Write-Output "=== Deployment Complete ==="
```

**Usage**:
```powershell
# Deploy Debug version
.\deploy.ps1

# Deploy Release version
.\deploy.ps1 -Configuration Release
```

---

**Document Version**: 1.0
**Last Updated**: 2025-10-19
**Next Review**: Before production release
