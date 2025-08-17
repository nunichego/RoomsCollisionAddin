# Simple Window Template - Aukett + Heese Revit Add-In 2024

## Overview

The Simple Window Template provides a complete, scalable foundation for all simple windows in the Aukett + Heese Revit Add-In. It includes comprehensive settings management, navigation, and configuration functionality that can be easily extended for new features.

## Features

### 🎨 **Visual Design System**
- **Consistent Branding**: Aukett + Heese color palette and typography
- **Modern UI**: Clean navigation tabs, organized settings groups, professional styling
- **Responsive Design**: Adapts to different content types and screen sizes
- **Professional Styling**: Rounded corners, hover effects, consistent spacing

### 🔧 **Built-in Functionality**
- **Tab Navigation**: Left-side navigation with active tab indicators
- **Settings Management**: Automatic change tracking and unsaved changes detection
- **Status System**: Dynamic status indicators and messaging
- **Event System**: Comprehensive event handling for all interactions

### 📱 **Scalable Architecture**
- **Modular Components**: Reusable styles and templates
- **Configurable UI**: Show/hide elements based on requirements
- **Extensible Design**: Easy to add new tabs and settings
- **Data Binding**: Full MVVM support with property change notifications

## File Structure

```
Resources/templates/SimpleWindow/
├── SimpleWindowTemplate.xaml        # Main XAML template
├── SimpleWindowTemplate.xaml.cs     # Code-behind with base functionality
├── ExampleSimpleWindow.xaml         # Example implementation
├── ExampleSimpleWindow.xaml.cs      # Example code-behind
└── README.md                        # This documentation
```

## Quick Start Guide

### 1. Create a New Simple Window

**Note**: All Simple Window Template files are located in the `SimpleWindow/` subfolder:
- `SimpleWindow/SimpleWindowTemplate.xaml` - Main XAML template
- `SimpleWindow/SimpleWindowTemplate.xaml.cs` - Code-behind with base functionality
- `SimpleWindow/ExampleSimpleWindow.xaml` - Example implementation
- `SimpleWindow/ExampleSimpleWindow.xaml.cs` - Example code-behind

```csharp
// Create a new XAML file based on the template
public partial class MySimpleWindow : SimpleWindowTemplate
{
    public MySimpleWindow()
    {
        InitializeComponent();
        
        // Configure the window
        WindowTitle = "My Simple Window - Aukett + Heese";
        
        // Set up event handlers
        SettingsSaved += MySimpleWindow_SettingsSaved;
        SettingsCancelled += MySimpleWindow_SettingsCancelled;
    }
}
```

### 2. Add Tabs and Content

```csharp
protected override void OnWindowLoaded()
{
    base.OnWindowLoaded();
    
    // Add tabs to the simple window
    AddTab("General", "General Settings", GeneralSettingsPanel);
    AddTab("Security", "Security", SecurityPanel);
    AddTab("Advanced", "Advanced", AdvancedPanel);
    AddTab("About", "About", AboutPanel);
    
    // Load settings
    LoadSettings();
}
```

### 3. Override Base Methods

```csharp
protected override void LoadSettings()
{
    try
    {
        // Load settings from your configuration service
        var settings = ConfigurationService.Instance.LoadConfiguration<MySettings>("my-settings.json");
        
        // Apply settings to UI
        ApplySettingsToUI(settings);
    }
    catch (Exception ex)
    {
        ShowError($"Failed to load settings: {ex.Message}");
    }
}

protected override void SaveSettings()
{
    try
    {
        // Collect settings from UI
        var settings = CollectSettingsFromUI();
        
        // Save settings to your configuration service
        ConfigurationService.Instance.SaveConfiguration(settings, "my-settings.json");
        
        ShowSuccess("Settings saved successfully");
        HasUnsavedChanges = false;
    }
    catch (Exception ex)
    {
        ShowError($"Failed to save settings: {ex.Message}");
        throw;
    }
}

protected override void OnHelpRequested()
{
    MessageBox.Show("Help information for your admin panel", "Help", MessageBoxButton.OK, MessageBoxImage.Information);
}
```

### 4. Handle Events

```csharp
private void MySimpleWindow_SettingsSaved(object sender, SettingsSavedEventArgs e)
{
    // Additional logic after settings are saved
    Console.WriteLine("Settings saved successfully");
}

private void MySimpleWindow_SettingsCancelled(object sender, SettingsCancelledEventArgs e)
{
    // Additional logic after settings are cancelled
    Console.WriteLine("Settings changes were cancelled");
}
```

## Configuration Options

### UI Properties

| Property | Description | Default |
|----------|-------------|---------|
| `CurrentTab` | Current active tab | `""` |
| `HasUnsavedChanges` | Indicates unsaved changes | `false` |
| `IsLoading` | Show loading state | `false` |
| `HasError` | Show error state | `false` |
| `ErrorMessage` | Error message when HasError is true | `""` |
| `StatusText` | Status message in footer | `"Ready"` |
| `StatusColor` | Status indicator color | `Green` |
| `WindowTitle` | Window title | `"Simple Window - Aukett + Heese"` |

### Navigation Properties

| Property | Description | Type |
|----------|-------------|------|
| `Tabs` | List of available tabs | `List<AdminTabInfo>` |
| `CurrentTab` | Current active tab | `string` |

## Style System

### Color Palette

```xml
<!-- Primary Colors -->
<SolidColorBrush x:Key="PrimaryBlue" Color="#0078D4"/>
<SolidColorBrush x:Key="PrimaryBlueDark" Color="#005A9E"/>
<SolidColorBrush x:Key="PrimaryBlueDarker" Color="#004578"/>

<!-- Secondary Colors -->
<SolidColorBrush x:Key="SecondaryGray" Color="#666666"/>
<SolidColorBrush x:Key="LightGray" Color="#F8F9FA"/>

<!-- Status Colors -->
<SolidColorBrush x:Key="SuccessGreen" Color="#28A745"/>
<SolidColorBrush x:Key="WarningOrange" Color="#FFC107"/>
<SolidColorBrush x:Key="ErrorRed" Color="#DC3545"/>
```

### Typography Styles

| Style | Usage | Font Size | Weight |
|-------|-------|-----------|--------|
| `SectionHeaderStyle` | Section headers | 18px | Bold |
| `SettingGroupStyle` | Group box headers | 14px | SemiBold |

### Button Styles

| Style | Usage | Appearance |
|-------|-------|------------|
| `ModernButtonStyle` | Primary actions | Blue gradient |
| `SecondaryButtonStyle` | Secondary actions | Light blue with border |
| `NavigationTabStyle` | Navigation tabs | Transparent with hover |
| `ActiveNavigationTabStyle` | Active navigation tab | Blue background with border |

### Input Styles

| Style | Usage | Features |
|-------|-------|----------|
| `ModernTextBoxStyle` | Text input fields | Rounded corners, focus highlight |
| `ModernCheckBoxStyle` | Checkbox controls | Consistent spacing |
| `ModernComboBoxStyle` | Dropdown controls | Rounded corners |

## Event System

### Available Events

```csharp
// Navigation changes
NavigationChanged += (sender, e) => {
    var tabName = e.TabName;
    // Handle navigation change
};

// Settings saved
SettingsSaved += (sender, e) => {
    // Handle settings saved
};

// Settings cancelled
SettingsCancelled += (sender, e) => {
    // Handle settings cancelled
};
```

### Virtual Methods to Override

```csharp
protected virtual void OnWindowLoaded() { }
protected virtual void OnWindowClosing(CancelEventArgs e) { }
protected virtual void LoadSettings() { }
protected virtual void SaveSettings() { }
protected virtual void OnHelpRequested() { }
protected virtual void OnSettingsSaved() { }
protected virtual void OnSettingsCancelled() { }
protected virtual void OnNavigationChanged(string tabName) { }
```

## Public Methods

### Tab Management

```csharp
// Add a new tab
AddTab(string name, string displayName, UIElement content);

// Remove a tab
RemoveTab(string name);
```

### Status Management

```csharp
// Show error message
ShowError(string message);

// Show success message
ShowSuccess(string message);

// Show warning message
ShowWarning(string message);

// Mark as modified
MarkAsModified();
```

## Advanced Customization

### Custom Tab Content

```xml
<!-- Create custom tab content -->
<Grid x:Name="MyCustomPanel" Visibility="Collapsed">
    <StackPanel>
        <TextBlock Text="My Custom Settings" Style="{StaticResource SectionHeaderStyle}"/>
        
        <GroupBox Header="Custom Settings" Style="{StaticResource SettingGroupStyle}">
            <StackPanel>
                <!-- Your custom controls here -->
                <CheckBox x:Name="MyCheckBox" 
                          Content="My Setting" 
                          Style="{StaticResource ModernCheckBoxStyle}"/>
                
                <TextBox x:Name="MyTextBox" 
                         Style="{StaticResource ModernTextBoxStyle}"/>
            </StackPanel>
        </GroupBox>
    </StackPanel>
</Grid>
```

### Custom Styling

```xml
<!-- Override specific styles -->
<Style x:Key="CustomButtonStyle" TargetType="Button" BasedOn="{StaticResource ModernButtonStyle}">
    <Setter Property="Background" Value="LightGreen"/>
    <Setter Property="CornerRadius" Value="8"/>
</Style>
```

### Change Tracking

```csharp
// Set up change tracking for controls
private void SetupChangeTracking()
{
    MyCheckBox.Checked += OnSettingChanged;
    MyCheckBox.Unchecked += OnSettingChanged;
    MyTextBox.TextChanged += OnSettingChanged;
}

private void OnSettingChanged(object sender, EventArgs e)
{
    MarkAsModified();
}
```

## Best Practices

### 1. **Consistent Naming**
- Use descriptive names for your simple windows
- Follow the pattern: `FeatureNameSimpleWindow`
- Keep namespaces consistent: `AukettHeeseRevitAddin2024.Windows.FeatureName`

### 2. **Error Handling**
- Always wrap operations in try-catch blocks
- Use the built-in error state system
- Provide meaningful error messages

### 3. **Settings Management**
- Implement proper load/save methods
- Use change tracking for all controls
- Handle unsaved changes appropriately

### 4. **User Experience**
- Provide clear status feedback
- Use appropriate loading states
- Handle errors gracefully

### 5. **Navigation**
- Use descriptive tab names
- Organize related settings together
- Keep navigation simple and intuitive

## Migration Guide

### From Existing Simple Windows

1. **Copy the template files** to your new simple window location
2. **Update the class name** and namespace
3. **Configure the window properties** (title, etc.)
4. **Move your existing logic** to the appropriate virtual methods
5. **Update your XAML** to use the template structure
6. **Test thoroughly** to ensure all functionality works

### Example Migration

**Before:**
```csharp
public partial class OldSimpleWindow : Window
{
    private void LoadData() { /* ... */ }
    private void SaveData() { /* ... */ }
    private void RefreshButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
}
```

**After:**
```csharp
public partial class NewSimpleWindow : SimpleWindowTemplate
{
    protected override void LoadSettings()
    {
        LoadData(); // Your existing logic
    }
    
    protected override void SaveSettings()
    {
        SaveData(); // Your existing logic
    }
}
```

## Troubleshooting

### Common Issues

1. **Tabs not showing**: Ensure tabs are added in OnWindowLoaded
2. **Settings not saving**: Check that SaveSettings is properly implemented
3. **Navigation not working**: Verify tab names match between AddTab calls
4. **Styles not applying**: Ensure the template is properly referenced

### Debug Tips

- Use the built-in error handling system
- Check the Output window for binding errors
- Verify all required properties are set
- Test with different settings scenarios

## Future Enhancements

### Planned Features
- **Theme System**: Dark/light mode support
- **Validation System**: Input validation framework
- **Accessibility Tools**: Enhanced accessibility features
- **Internationalization**: Multi-language support
- **Plugin System**: Extensible settings system

### Contribution Guidelines
- Follow the existing code style
- Add comprehensive documentation
- Include unit tests for new features
- Test across different scenarios

---

## Support

For questions or issues with the Simple Window Template:
1. Check this documentation first
2. Review the existing implementations
3. Consult the development team
4. Create an issue in the project repository

**Last Updated**: Current Session  
**Version**: 1.0  
**Maintainer**: Aukett + Heese Development Team
