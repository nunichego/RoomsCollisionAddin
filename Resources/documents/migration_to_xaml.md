# Migration to XAML – Field Manual (August 27, 2025)

This is a repeatable guide for teammates to migrate an existing Revit add-in UI from pure programmatic WPF to a XAML window shell without breaking anything.

## What you’ll achieve
- Keep existing programmatic controls (filters, AND/OR sets/rules) working exactly as-is
- Add a XAML window shell for maintainability and future UX work
- Build with `dotnet build` (no Visual Studio/MSBuild dependency) and deploy cleanly

## Prerequisites
- Project targets .NET Framework 4.8 (Revit 2024)
- You can run PowerShell and `dotnet` CLI
- Revit 2024 API assemblies referenced (non-copy-local)

## Quick checklist (10 minutes)
1) Add XAML window `RoomWallAnalysisWindow.xaml`
2) In code-behind, call `InitializeComponent()`, then embed your existing programmatic panel into a named host Grid
3) Set Revit as Owner via `WindowInteropHelper` when showing the window
4) Switch project to SDK-style with `<UseWPF>true</UseWPF>` (or use MSBuild if staying classic)
5) Exclude templates/samples from compilation
6) Resolve `Grid` ambiguity by fully qualifying `System.Windows.Controls.Grid`
7) Build with `dotnet build -c Debug`, deploy DLL + .addin to Revit addins folder

## Step-by-step

1) Owner hookup (required for Revit)
- In your command before `ShowDialog()`:
```csharp
// using System.Windows.Interop;
var window = new RoomWallAnalysisWindow(document);
new WindowInteropHelper(window) { Owner = uiApp.MainWindowHandle };
window.ShowDialog();
```

2) Create a XAML shell and host existing UI
- Create `RoomWallAnalysisWindow.xaml` with a named host Grid (e.g., `LeftHost`)
- In `RoomWallAnalysisWindow.xaml.cs`:
```csharp
InitializeComponent();
var filterPanel = new FilterRulesPanel(_controller);
filterPanel.FilterChanged += OnFilterChanged; // If needed
var leftHost = this.FindName("LeftHost") as System.Windows.Controls.Grid;
leftHost?.Children.Add(filterPanel);
```

3) Switch to SDK-style WPF (recommended)
- Replace .csproj with:
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <UseWPF>true</UseWPF>
    <OutputType>Library</OutputType>
    <RootNamespace>YourNamespace</RootNamespace>
    <AssemblyName>YourAssemblyName</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="RevitAPI">
      <HintPath>C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="RevitAPIUI">
      <HintPath>C:\Program Files\Autodesk\Revit 2024\RevitAPIUI.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
</Project>
```
- Why: ensures XAML compiles with `dotnet build`. If you must keep a classic csproj, use `msbuild` instead of `dotnet build`.

4) Exclude template/sample content
- Add to .csproj (SDK-style):
```xml
<ItemGroup>
  <Compile Remove="Resources\templates\**\*.cs" />
  <Page Remove="Resources\templates\**\*.xaml" />
  <None Remove="Resources\templates\**\*" />
  <EmbeddedResource Remove="Resources\templates\**\*" />
</ItemGroup>
```

5) Resolve `Grid` ambiguity (Revit vs WPF)
- Use the fully-qualified WPF type where needed:
```csharp
var leftHost = this.FindName("LeftHost") as System.Windows.Controls.Grid;
```

6) Build & deploy
- Build:
```powershell
dotnet build -c Debug
```
- Deploy to Revit 2024 addins folder:
```powershell
$dest = "$env:AppData\Autodesk\Revit\Addins\2024"
if (!(Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
Copy-Item -Force "bin\Debug\net48\YourAssemblyName.dll" $dest
Copy-Item -Force "YourAssemblyName.addin" $dest
```

## Troubleshooting (what we hit and how to fix)
- `InitializeComponent` missing / XAML names not found
  - Use SDK-style with `<UseWPF>true</UseWPF>` (or build with msbuild.exe for classic projects)
- XAML parse errors from sample templates
  - Exclude `Resources/templates/**` from Compile/Page/None/EmbeddedResource
- Duplicate `ProgressWindow` type/members
  - Ensure you compile only one implementation (exclude `.xaml.cs` or remove the extra file)
- `Grid` is ambiguous between WPF and Revit
  - Qualify WPF type as `System.Windows.Controls.Grid`

## Verification
- Add a trivial UI change (e.g., a dummy button) and confirm it appears
- Launch the add-in; the window should be modal and stay on top of Revit
- All filters (AND/OR, sets, rules) should work identically

## Rollback plan
- Revert the .csproj change to classic if needed
- Remove the XAML window and use the original programmatic window
- Keep the owner hookup—it’s safe and improves UX even without XAML

## Notes for future migrations
- Start with a shell-only XAML migration; keep business logic and custom controls unchanged
- Move visuals/styling to XAML incrementally (ResourceDictionaries, styles)
- Consider MVVM only if you plan significant UI evolution; otherwise keep current controller-based flow

## Final status (this project)
- XAML shell in place; programmatic filter UI preserved
- Green build with `dotnet build`; verified in Revit 2024
