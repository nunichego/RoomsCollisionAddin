# Aukett + Heese Revit Add-in Template - Setup Instructions

## 🚀 Quick Start Guide (5 minutes)

### Step 1: Copy Template Files
1. Copy the entire `AukettHeeseAddinTemplate` folder to your new project location
2. Rename the folder to your project name (e.g., `MyAwesomeAddin`)

### Step 2: Rename and Customize Files

#### 2.1 Rename Project Files
- `YourAddinName.csproj` → `MyAwesomeAddin.csproj`
- `YourAddinName.addin` → `MyAwesomeAddin.addin`

#### 2.2 Update All File Contents
**Search and replace** the following in **ALL FILES**:
- `YourAddinName` → `MyAwesomeAddin` (your actual project name)
- `"Your Panel Name"` → `"My Panel"` (your panel name in App.cs)
- `"YourAddinName.dll"` → `"MyAwesomeAddin.dll"` (in App.cs)

#### 2.3 Generate New GUID
In your `.addin` file, replace:
```xml
<ClientId>{GENERATE-NEW-GUID-HERE}</ClientId>
```
With a new GUID (use Visual Studio > Tools > Create GUID or online generator).

### Step 3: Build and Deploy
Open PowerShell in your project folder and run:
```powershell
.\build-and-deploy.ps1
```

### Step 4: Test in Revit
1. **Restart Revit 2024 completely**
2. Look for the **"Aukett + Heese"** tab in the ribbon
3. Find your panel with the "Hello World" button
4. Click it to verify it works!

---

## 📝 Detailed Customization Guide

### Updating the Panel and Button

#### Change Panel Name
In `App.cs`, line ~23:
```csharp
private const string PANEL_NAME = "My Custom Panel";
```

#### Change Button Properties
In `App.cs`, `AddHelloWorldButton()` method:
```csharp
var buttonData = new PushButtonData(
    "MyCommand",           // Internal ID
    "My Button",           // Display text
    _assemblyPath,
    "MyAwesomeAddin.Commands.MyCommand"  // Command class
);

buttonData.ToolTip = "My custom tooltip";
buttonData.LongDescription = "Detailed description here";
```

#### Add Your Own Command
1. Rename `HelloWorldCommand.cs` to `MyCommand.cs`
2. Update the class name and namespace
3. Implement your functionality in the `Execute` method

### Adding More Buttons

Add this to `CreateRibbonPanel()` method in `App.cs`:
```csharp
// After AddHelloWorldButton(panel);
AddMySecondButton(panel);
```

Then create the method:
```csharp
private void AddMySecondButton(RibbonPanel panel)
{
    var buttonData = new PushButtonData(
        "SecondCommand",
        "Second Button", 
        _assemblyPath,
        "MyAwesomeAddin.Commands.SecondCommand"
    );
    
    // Set properties...
    var button = panel.AddItem(buttonData) as PushButton;
}
```

### Using Different Icons

1. Add your icon files to `Resources/icons/`
2. Update the project file:
```xml
<EmbeddedResource Include="Resources\icons\myicon-32.png" />
```
3. Update icon loading in `AddHelloWorldButton()`:
```csharp
var icon = LoadIcon("myicon-32.png");
```

---

## 🛠️ Build Scripts Reference

### Individual Scripts
- **`build.ps1`** - Only builds the project
- **`deploy.ps1`** - Only deploys (requires build first)

### Combined Script
- **`build-and-deploy.ps1`** - Does both build and deploy

### Script Features
✅ Automatic project file detection  
✅ Build verification  
✅ Deployment verification  
✅ Timestamp checking  
✅ Clear status messages  
✅ Error handling  

---

## 🔧 Troubleshooting

### "Build failed" errors
1. Check that Revit 2024 is installed
2. Verify the Revit API paths in the `.csproj` file
3. Ensure all file names and namespaces match

### "Tab already exists" error
**This template handles this automatically!** The error should not occur because of the exception handling in `App.cs`.

### Button doesn't appear
1. Check the `.addin` file is in the right location
2. Verify the `ClientId` is unique
3. Make sure the `Assembly` and `FullClassName` paths are correct
4. Restart Revit completely

### Wrong icon or no icon
1. Verify icon files are in `Resources/icons/`
2. Check they're marked as `EmbeddedResource` in the project file
3. Verify the resource name in `LoadIcon()` method

---

## 📁 Final Project Structure

```
MyAwesomeAddin/
├── MyAwesomeAddin.csproj      # Project file
├── MyAwesomeAddin.addin       # Revit manifest  
├── App.cs                     # Main application class
├── Commands/
│   └── HelloWorldCommand.cs  # Your command(s)
├── Resources/
│   └── icons/
│       ├── help-16.png        # Small icon
│       └── help-32.png        # Large icon
├── build.ps1                  # Build script
├── deploy.ps1                 # Deploy script
├── build-and-deploy.ps1       # Combined script
├── README.md                  # Template documentation
└── SETUP-INSTRUCTIONS.md     # This file
```

---

## 🎯 Key Benefits of This Template

✅ **Shared Tab Support** - Properly handles existing "Aukett + Heese" tab  
✅ **Error-Safe** - Won't crash if tab already exists  
✅ **Automated Scripts** - One-click build and deploy  
✅ **Ready Icons** - Includes help icons to get started  
✅ **Modern Project** - SDK-style project with proper references  
✅ **Clear Structure** - Organized folders and files  

---

**💡 Pro Tip**: After you get the template working, copy it again for your next add-in. You'll have a proven base to work from!