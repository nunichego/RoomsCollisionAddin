# WPF/XAML in Revit Add-ins - Complete Guide

**Based on real migration experience from programmatic WPF to XAML in Revit 2024 add-ins**

This guide solves the recurring struggles with WPF/XAML setup in Revit add-ins. Follow this and you'll have working XAML windows in 10 minutes.

---

## 🎯 Quick Win: XAML Template Window

### 1. Create Your XAML Window

Create `MyWindow.xaml`:
```xml
<Window x:Class="YourAddinName.MyWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="My Add-in Window" 
        Width="800" Height="600"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Main content area -->
        <Grid Grid.Row="0" x:Name="MainContent">
            <TextBlock Text="Your content goes here" 
                       HorizontalAlignment="Center" 
                       VerticalAlignment="Center"
                       FontSize="16"/>
        </Grid>
        
        <!-- Button bar -->
        <StackPanel Grid.Row="1" 
                    Orientation="Horizontal" 
                    HorizontalAlignment="Right" 
                    Margin="10">
            <Button Name="OkButton" Content="OK" Width="75" Margin="5" Click="OkButton_Click"/>
            <Button Name="CancelButton" Content="Cancel" Width="75" Margin="5" Click="CancelButton_Click"/>
        </StackPanel>
    </Grid>
</Window>
```

### 2. Create Code-Behind

Create `MyWindow.xaml.cs`:
```csharp
using System;
using System.Windows;
using Autodesk.Revit.DB;

namespace YourAddinName
{
    public partial class MyWindow : Window
    {
        private readonly Document _document;

        public MyWindow(Document document)
        {
            InitializeComponent();  // This is CRITICAL for XAML
            _document = document;
            
            // Your initialization logic here
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Your OK logic here
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
```

### 3. Show Window from Your Command

In your Revit command:
```csharp
using System.Windows.Interop;
using Autodesk.Revit.UI;

public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
{
    try
    {
        var uiApp = commandData.Application;
        var doc = uiApp.ActiveUIDocument?.Document;
        
        if (doc == null)
        {
            TaskDialog.Show("Error", "No active document");
            return Result.Failed;
        }

        // Create and show window with proper Revit ownership
        var window = new MyWindow(doc);
        
        // CRITICAL: Set Revit as owner window
        new WindowInteropHelper(window) { Owner = uiApp.MainWindowHandle };
        
        // Show as modal dialog
        var result = window.ShowDialog();
        
        return result == true ? Result.Succeeded : Result.Cancelled;
    }
    catch (Exception ex)
    {
        message = ex.Message;
        return Result.Failed;
    }
}
```

---

## 🔧 Project Setup - The Critical Parts

### Project File Configuration (.csproj)

**This is where most people get stuck!** Use this exact structure:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <UseWPF>true</UseWPF>  <!-- CRITICAL FOR XAML -->
    <OutputType>Library</OutputType>
    <RootNamespace>YourAddinName</RootNamespace>
    <AssemblyName>YourAddinName</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <PlatformTarget>x64</PlatformTarget>
    <Prefer32Bit>false</Prefer32Bit>
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

  <!-- Exclude templates and examples from build -->
  <ItemGroup>
    <Compile Remove="Resources\templates\**\*.cs" />
    <Page Remove="Resources\templates\**\*.xaml" />
    <None Remove="Resources\templates\**\*" />
    <EmbeddedResource Remove="Resources\templates\**\*" />
  </ItemGroup>
</Project>
```

**Key Points:**
- `Sdk="Microsoft.NET.Sdk.WindowsDesktop"` - Enables WPF
- `<UseWPF>true</UseWPF>` - Critical for XAML compilation
- `<Private>False</Private>` for Revit APIs - Don't copy to output

---

## 🚨 Common Issues & Solutions

### Issue 1: "InitializeComponent() doesn't exist"

**Problem**: XAML isn't being compiled  
**Solution**: Use SDK-style project with `<UseWPF>true</UseWPF>`

### Issue 2: "The name 'MyButton' does not exist in current context"

**Problem**: XAML elements not accessible in code-behind  
**Solution**: 
1. Ensure `x:Name` is set in XAML: `<Button x:Name="MyButton" .../>`
2. Build with `dotnet build` (not `msbuild` for classic projects)

### Issue 3: "Grid is ambiguous" error

**Problem**: Both Revit API and WPF have `Grid` classes  
**Solution**: Use fully qualified names:
```csharp
// Instead of: Grid myGrid
System.Windows.Controls.Grid myGrid = new System.Windows.Controls.Grid();
```

### Issue 4: XAML Parse Exceptions

**Problem**: Template files interfering with build  
**Solution**: Exclude them in project file:
```xml
<ItemGroup>
  <Compile Remove="Resources\templates\**\*.cs" />
  <Page Remove="Resources\templates\**\*.xaml" />
</ItemGroup>
```

### Issue 5: Window Not Modal / Behind Revit

**Problem**: Missing owner window setup  
**Solution**: Always set Revit as owner:
```csharp
new WindowInteropHelper(window) { Owner = uiApp.MainWindowHandle };
```

---

## 🎨 Advanced XAML Patterns

### Hosting Existing Programmatic Controls

If you have existing WPF controls created in code:
```csharp
public MyWindow()
{
    InitializeComponent();
    
    // Create your existing programmatic control
    var myExistingPanel = new MyProgrammaticPanel();
    
    // Find the host grid in XAML and add the control
    var host = this.FindName("MainContent") as System.Windows.Controls.Grid;
    host?.Children.Add(myExistingPanel);
}
```

### Data Binding Setup

```xml
<Window x:Class="YourAddinName.MyWindow" DataContext="{Binding RelativeSource={RelativeSource Self}}">
    <TextBox Text="{Binding MyProperty}" />
</Window>
```

```csharp
public partial class MyWindow : Window, INotifyPropertyChanged
{
    private string _myProperty;
    public string MyProperty
    {
        get => _myProperty;
        set
        {
            _myProperty = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### Custom Styles and Resources

Create `Styles.xaml`:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="#2196F3"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="10,5"/>
        <Setter Property="BorderThickness" Value="0"/>
    </Style>
    
</ResourceDictionary>
```

Include in your window:
```xml
<Window.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Window.Resources>
```

---

## ⚡ Build and Deploy Scripts for XAML

Update your `build.ps1` to handle XAML:
```powershell
Write-Host "Building Revit Add-in with XAML..." -ForegroundColor Green

try {
    # Build using dotnet (handles XAML compilation)
    dotnet build --configuration Debug --verbosity minimal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build with XAML completed successfully!" -ForegroundColor Green
    } else {
        Write-Error "Build failed - check XAML compilation errors"
        exit 1
    }
} catch {
    Write-Error "Build error: $($_.Exception.Message)"
    exit 1
}
```

---

## 🏗️ Migration from Programmatic WPF

### Step-by-Step Migration

1. **Keep existing programmatic controls** - Don't rewrite everything
2. **Create XAML shell** - Just the window layout
3. **Host existing controls** in XAML containers
4. **Test thoroughly** - Ensure all functionality works
5. **Migrate incrementally** - Move to XAML piece by piece

### Migration Example

**Before (Pure Code):**
```csharp
var window = new Window();
var grid = new Grid();
var button = new Button { Content = "Click Me" };
grid.Children.Add(button);
window.Content = grid;
window.ShowDialog();
```

**After (XAML Shell + Code):**
```xml
<!-- MyWindow.xaml -->
<Window ...>
    <Grid x:Name="MainGrid">
        <!-- Existing controls hosted here -->
    </Grid>
</Window>
```

```csharp
// MyWindow.xaml.cs
public MyWindow()
{
    InitializeComponent();
    
    // Host your existing programmatic controls
    var myExistingControl = new MyProgrammaticControl();
    MainGrid.Children.Add(myExistingControl);
}
```

---

## 🎯 Best Practices

### ✅ Do This
- Always use `WindowInteropHelper` to set Revit as owner
- Use SDK-style projects with `<UseWPF>true</UseWPF>`
- Build with `dotnet build` for XAML projects
- Keep existing business logic - only migrate UI gradually
- Test after each small change

### ❌ Avoid This
- Don't migrate everything to XAML at once
- Don't forget `InitializeComponent()` in constructors
- Don't use `msbuild` with SDK-style WPF projects
- Don't include sample/template XAML files in build
- Don't forget the Revit owner window setup

---

## 🔍 Troubleshooting Checklist

When XAML isn't working:

- [ ] Project uses `Microsoft.NET.Sdk.WindowsDesktop`?
- [ ] `<UseWPF>true</UseWPF>` in project file?
- [ ] `InitializeComponent()` called in constructor?
- [ ] Building with `dotnet build` (not `msbuild`)?
- [ ] Template files excluded from compilation?
- [ ] Revit set as owner window?
- [ ] No naming conflicts (Grid, etc.)?

---

**🏆 Result**: Working XAML windows in Revit add-ins without the usual headaches!

This guide is battle-tested from real Revit add-in projects. Follow it and save hours of debugging XAML setup issues.