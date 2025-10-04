# Aukett + Heese Revit Add-in Template

This template creates a minimal "Hello World" Revit add-in that properly handles the shared "Aukett + Heese" ribbon tab.

## Quick Start

1. **Copy template files** to your new project folder
2. **Rename files and namespaces** to match your project
3. **Update the ClientId** in the .addin file (generate a new GUID)
4. **Run build and deploy scripts**

## Template Files

```
YourAddinName/
├── App.cs                     # Main application class with shared tab handling
├── Commands/
│   └── HelloWorldCommand.cs   # Sample command
├── YourAddinName.csproj       # Project file with Revit references
├── YourAddinName.addin        # Revit add-in manifest
├── Resources/
│   └── icons/
│       ├── help-16.png        # 16x16 icon
│       └── help-32.png        # 32x32 icon
├── build.ps1                  # PowerShell build script
├── deploy.ps1                 # PowerShell deploy script
└── build-and-deploy.ps1       # Combined build and deploy script
```

## Customization Steps

### 1. Rename Project Files
- Rename `YourAddinName.csproj` to `[YourProjectName].csproj`
- Rename `YourAddinName.addin` to `[YourProjectName].addin`

### 2. Update Namespaces and Class Names
Replace `YourAddinName` with your actual project name in:
- `App.cs` - namespace and class references
- `Commands/HelloWorldCommand.cs` - namespace
- `YourAddinName.csproj` - RootNamespace and AssemblyName
- `YourAddinName.addin` - Assembly and FullClassName

### 3. Generate New ClientId
In the `.addin` file, replace the ClientId GUID with a new one:
```xml
<ClientId>{YOUR-NEW-GUID-HERE}</ClientId>
```
Use Visual Studio > Tools > Create GUID or online GUID generator.

### 4. Customize Button Properties
In `App.cs`, update:
- `PANEL_NAME` - Your panel name
- Button text, tooltip, and description
- Icon references if you want different icons

## Build and Deploy

### Option 1: Individual Scripts
```powershell
# Build only
.\build.ps1

# Deploy only (after building)
.\deploy.ps1
```

### Option 2: Combined Script
```powershell
# Build and deploy in one step
.\build-and-deploy.ps1
```

## Key Features

✅ **Shared Tab Handling** - Properly handles existing "Aukett + Heese" tab  
✅ **Error-Safe** - Won't crash if tab already exists  
✅ **Ready Icons** - Includes help icons (16px and 32px)  
✅ **Revit 2024** - Configured for Revit 2024 API  
✅ **Modern Project** - SDK-style project with .NET Framework 4.8  
✅ **Auto Deploy** - PowerShell scripts for easy deployment  

## Next Steps

1. Add your actual functionality to the HelloWorldCommand
2. Add more buttons/commands as needed
3. Update the README with your specific documentation
4. Consider adding icons specific to your functionality

## Troubleshooting

**"Tab already exists" error**: This template handles this automatically via exception catching.

**Buttons don't appear**: Check that the .addin file is in the correct location and the assembly path is correct.

**Build errors**: Ensure Revit 2024 is installed and the API references are correct.