# Modern Window Template - Aukett + Heese Revit Add-In 2024

## Overview

This templates directory contains comprehensive templates for Revit add-in development:

### **Revit Add-in Template** (`RevitAddin/`)
Complete foundation for Revit add-in development with modular architecture, dependency injection, comprehensive logging, and reusable UI templates. Includes ribbon interface, commands, services, and configuration management.

### **Modern Window Template** (`ModernWindow/`)
Complete, scalable foundation for content-rich application windows with search, filtering, and grid layouts. Includes comprehensive styling, layout management, and functionality that can be easily extended for new features.

### **Simple Window Template** (`SimpleWindow/`)
Foundation for settings and configuration windows with tab-based navigation, settings management, and consistent UI patterns.

## Template Categories

### 🏗️ **Revit Add-in Template** (`RevitAddin/`)
- **Modular Architecture**: Commands can be easily migrated between add-ins
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service management
- **Comprehensive Logging**: Microsoft.Extensions.Logging with multiple providers
- **Configuration Management**: JSON-based configuration with validation and backup
- **Ribbon Interface**: Automatic ribbon creation with panels and commands
- **Service Layer**: Interface-based business logic services
- **Modern Patterns**: MVVM, async/await, and modern .NET practices

### 🎨 **Modern Window Template** (`ModernWindow/`)
- **Visual Design System**: Consistent branding with Aukett + Heese color palette
- **Modern UI**: Gradient headers, card-based layouts, smooth animations
- **Responsive Design**: Adapts to different content types and screen sizes
- **Professional Styling**: Drop shadows, rounded corners, hover effects

- **Professional Styling**: Drop shadows, rounded corners, hover effects
- **Built-in Functionality**: Search & filter, content management, status system
- **Scalable Architecture**: Modular components, configurable UI, extensible design
- **Data Binding**: Full MVVM support with property change notifications

### ⚙️ **Simple Window Template** (`SimpleWindow/`)
- **Tab Navigation**: Left-side navigation with active tab indicators
- **Settings Management**: Automatic change tracking and unsaved changes detection
- **Status System**: Dynamic status indicators and messaging
- **Event System**: Comprehensive event handling for all interactions
- **Professional Styling**: Modern UI elements with consistent spacing
- **Scalable Architecture**: Easy to add new tabs and settings
- **Data Binding**: Full MVVM support with property change notifications

## File Structure

```
Resources/templates/
├── ModernWindow/                      # Modern Window Template (COMPLETE FOLDER)
│   ├── ModernWindowTemplate.xaml      # Main XAML template
│   ├── ModernWindowTemplate.xaml.cs   # Code-behind with base functionality
│   ├── ExampleWindow.xaml             # Example implementation
│   ├── ExampleWindow.xaml.cs          # Example code-behind
│   └── README.md                      # Template documentation
├── SimpleWindow/                      # Simple Window Template (COMPLETE FOLDER)
│   ├── SimpleWindowTemplate.xaml      # Main XAML template
│   ├── SimpleWindowTemplate.xaml.cs   # Code-behind with base functionality
│   ├── ExampleSimpleWindow.xaml       # Example implementation
│   ├── ExampleSimpleWindow.xaml.cs    # Example code-behind
│   └── README.md                      # Template documentation
├── RevitAddin/                        # Revit Add-in Template (COMPLETE FOLDER)
│   ├── RevitAddinTemplate.csproj      # Project file with dependencies
│   ├── RevitAddinTemplate.addin       # Revit add-in manifest
│   ├── App.cs                         # Main application class
│   ├── Properties/AssemblyInfo.cs     # Assembly metadata
│   ├── Commands/                      # Modular command structure
│   │   ├── BaseCommand.cs             # Base command with common functionality
│   │   ├── AboutCommand.cs            # Example command implementation
│   │   ├── AdminPanelCommand.cs       # Admin panel command
│   │   ├── ContentFilesCommand.cs     # Content management command
│   │   ├── DynamoScriptsCommand.cs    # Dynamo scripts command
│   │   ├── HelpCommand.cs             # Help command
│   │   └── SettingsCommand.cs         # Settings command
│   ├── Services/                      # Business logic services
│   │   ├── IConfigurationService.cs   # Configuration service interface
│   │   └── ConfigurationService.cs    # JSON-based configuration service
│   ├── Models/                        # Data models
│   │   └── AppSettings.cs             # Application settings model
│   └── README.md                      # Template documentation
└── README.md                          # Main templates documentation
```

## Quick Start Guide

### 1. Create a New Revit Add-in Project

**For complete Revit add-in development:**
- Use the `RevitAddin/` template for full add-in projects
- Includes ribbon interface, commands, services, and configuration
- Supports modular architecture for easy command migration
- Includes dependency injection, logging, and modern patterns

**For individual windows:**
- Use `ModernWindow/` for content-rich windows with search and filtering
- Use `SimpleWindow/` for settings and configuration windows

### 2. Create a New Window

**Note**: All Modern Window Template files are located in the `ModernWindow/` subfolder:
- `ModernWindow/ModernWindowTemplate.xaml` - Main XAML template
- `ModernWindow/ModernWindowTemplate.xaml.cs` - Code-behind with base functionality
- `ModernWindow/ExampleWindow.xaml` - Example implementation
- `ModernWindow/ExampleWindow.xaml.cs` - Example code-behind
- `ModernWindow/README.md` - Detailed template documentation

```csharp
// Create a new XAML file based on the template
public partial class MyNewWindow : ModernWindowTemplate
{
    public MyNewWindow()
    {
        InitializeComponent();
        
        // Configure the window
        HeaderTitle = "🎯 My New Feature";
        HeaderSubtitle = "Description of what this window does";
        WindowTitle = "My New Feature - Aukett + Heese";
        
        // Show/hide UI elements as needed
        ShowAddButton = true;
        ShowSettingsButton = true;
        ShowHelpButton = true;
    }
}
```

### 2. Override Base Methods

```csharp
protected override void LoadContent()
{
    IsLoading = true;
    
    // Load your data here
    var items = LoadMyData();
    Items = items;
    
    IsLoading = false;
}

protected override void OnAddRequested()
{
    // Handle add new item
    MessageBox.Show("Add new item functionality");
}

protected override void OnContentItemSelected(object item, string action)
{
    if (action == "Open")
    {
        // Handle opening the item
        OpenItem(item);
    }
    else if (action == "ViewDetails")
    {
        // Handle viewing details
        ShowItemDetails(item);
    }
}
```

### 3. Customize the Item Template

Override the default item template in your XAML:

```xml
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <Border Style="{StaticResource ModernCardStyle}">
            <Grid>
                <!-- Your custom layout here -->
                <TextBlock Text="{Binding MyProperty}" 
                           Style="{StaticResource TitleTextStyle}"/>
                
                <!-- Custom action buttons -->
                <Button Content="Custom Action" 
                        Style="{StaticResource PrimaryButtonStyle}"
                        Click="CustomAction_Click"
                        Tag="{Binding}"/>
            </Grid>
        </Border>
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

## Configuration Options

### UI Visibility Properties

| Property | Description | Default |
|----------|-------------|---------|
| `ShowRefreshButton` | Show refresh button in header | `true` |
| `ShowSettingsButton` | Show settings button in header | `false` |
| `ShowToolbar` | Show search/filter toolbar | `true` |
| `ShowFilterButton` | Show filter button | `true` |
| `ShowSortButton` | Show sort button | `true` |
| `ShowAddButton` | Show add button | `false` |
| `ShowHelpButton` | Show help button in footer | `false` |

### Content Properties

| Property | Description | Default |
|----------|-------------|---------|
| `GridColumns` | Number of columns in grid layout | `3` |
| `Items` | Collection of items to display | `null` |
| `SearchText` | Current search text | `""` |
| `IsLoading` | Show loading state | `false` |
| `HasError` | Show error state | `false` |
| `IsEmpty` | Show empty state | `false` |

### Status Properties

| Property | Description | Default |
|----------|-------------|---------|
| `StatusText` | Status message in footer | `"Ready"` |
| `StatusColor` | Status indicator color | `Green` |
| `ErrorMessage` | Error message when HasError is true | `""` |
| `EmptyMessage` | Message when no content available | `"No content available."` |

## Style System

### Color Palette

```xml
<!-- Primary Colors -->
<SolidColorBrush x:Key="PrimaryBlue" Color="#0078D4"/>
<SolidColorBrush x:Key="PrimaryBlueDark" Color="#106EBE"/>
<SolidColorBrush x:Key="PrimaryBlueDarker" Color="#005A9E"/>

<!-- Secondary Colors -->
<SolidColorBrush x:Key="SecondaryGray" Color="#6C757D"/>
<SolidColorBrush x:Key="LightGray" Color="#F8F9FA"/>

<!-- Status Colors -->
<SolidColorBrush x:Key="SuccessGreen" Color="#28A745"/>
<SolidColorBrush x:Key="WarningOrange" Color="#FFC107"/>
<SolidColorBrush x:Key="ErrorRed" Color="#DC3545"/>
```

### Typography Styles

| Style | Usage | Font Size | Weight |
|-------|-------|-----------|--------|
| `HeaderTextStyle` | Main window title | 28px | Bold |
| `SubheaderTextStyle` | Window subtitle | 16px | Normal |
| `TitleTextStyle` | Card titles | 18px | Bold |
| `BodyTextStyle` | Descriptions | 13px | Normal |
| `StatusTextStyle` | Status messages | 13px | Normal |

### Button Styles

| Style | Usage | Appearance |
|-------|-------|------------|
| `PrimaryButtonStyle` | Main actions | Blue gradient |
| `SecondaryButtonStyle` | Secondary actions | Gray gradient |
| `LightSecondaryButtonStyle` | Toolbar buttons | Light gray |
| `HeaderButtonStyle` | Header actions | Transparent with white border |

### Card Styles

| Style | Usage | Features |
|-------|-------|----------|
| `ModernCardStyle` | Grid items | Hover animation, shadow |
| `ListItemCardStyle` | List items | No hover animation |

## Event System

### Available Events

```csharp
// Content selection
ContentItemSelected += (sender, e) => {
    var item = e.Item;
    var action = e.Action; // "Open", "ViewDetails", etc.
};

// Search
SearchPerformed += (sender, e) => {
    var searchText = e.SearchText;
    // Implement search logic
};

// Filter
FilterApplied += (sender, e) => {
    var filterType = e.FilterType;
    var filterValue = e.FilterValue;
    // Implement filter logic
};

// Sort
SortApplied += (sender, e) => {
    var sortProperty = e.SortProperty;
    var ascending = e.Ascending;
    // Implement sort logic
};
```

### Virtual Methods to Override

```csharp
protected virtual void OnWindowLoaded() { }
protected virtual void OnWindowClosing(CancelEventArgs e) { }
protected virtual void LoadContent() { }
protected virtual void RefreshContent() { }
protected virtual void OnSettingsRequested() { }
protected virtual void OnFilterRequested() { }
protected virtual void OnSortRequested() { }
protected virtual void OnAddRequested() { }
protected virtual void OnHelpRequested() { }
protected virtual void OnContentItemSelected(object item, string action) { }
```

## Advanced Customization

### Custom Layout Templates

```xml
<!-- Use list layout instead of grid -->
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <StackPanel Orientation="Vertical"/>
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

### Custom State Management

```csharp
// Show loading state
IsLoading = true;
StatusText = "Loading data...";

// Show error state
HasError = true;
ErrorMessage = "Failed to load data";
StatusText = "Error occurred";

// Show empty state
IsEmpty = true;
EmptyMessage = "No items found";
StatusText = "No content available";
```

### Custom Styling

```xml
<!-- Override specific styles -->
<Style x:Key="CustomCardStyle" TargetType="Border" BasedOn="{StaticResource ModernCardStyle}">
    <Setter Property="Background" Value="LightBlue"/>
    <Setter Property="CornerRadius" Value="20"/>
</Style>
```

## Best Practices

### 1. **Consistent Naming**
- Use descriptive names for your windows
- Follow the pattern: `FeatureNameWindow`
- Keep namespaces consistent: `AukettHeeseRevitAddin2024.Windows.FeatureName`

### 2. **Error Handling**
- Always wrap operations in try-catch blocks
- Use the built-in error state system
- Provide meaningful error messages

### 3. **Performance**
- Load data asynchronously when possible
- Use virtualization for large lists
- Implement proper disposal of resources

### 4. **User Experience**
- Provide clear status feedback
- Use appropriate loading states
- Handle empty states gracefully

### 5. **Accessibility**
- Use semantic button names
- Provide keyboard navigation
- Include proper tooltips

## Migration Guide

### From Existing Windows

1. **Copy the template files** to your new window location
2. **Update the class name** and namespace
3. **Configure the window properties** (title, subtitle, etc.)
4. **Move your existing logic** to the appropriate virtual methods
5. **Update your XAML** to use the template structure
6. **Test thoroughly** to ensure all functionality works

### Example Migration

**Before:**
```csharp
public partial class OldWindow : Window
{
    private void LoadData() { /* ... */ }
    private void RefreshButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
}
```

**After:**
```csharp
public partial class NewWindow : ModernWindowTemplate
{
    protected override void LoadContent()
    {
        LoadData(); // Your existing logic
    }
    
    protected override void RefreshContent()
    {
        LoadData(); // Your existing logic
    }
}
```

## Troubleshooting

### Common Issues

1. **Styles not applying**: Ensure the template is properly referenced
2. **Data binding errors**: Check that DataContext is set correctly
3. **Events not firing**: Verify event handlers are properly connected
4. **Performance issues**: Use async loading and virtualization

### Debug Tips

- Use the built-in error handling system
- Check the Output window for binding errors
- Verify all required properties are set
- Test with different data scenarios

## Future Enhancements

### Planned Features
- **Theme System**: Dark/light mode support
- **Animation Library**: More sophisticated animations
- **Accessibility Tools**: Enhanced accessibility features
- **Internationalization**: Multi-language support
- **Plugin System**: Extensible component system

### Contribution Guidelines
- Follow the existing code style
- Add comprehensive documentation
- Include unit tests for new features
- Test across different scenarios

---

## Support

For questions or issues with the Modern Window Template:
1. Check this documentation first
2. Review the existing implementations
3. Consult the development team
4. Create an issue in the project repository

**Last Updated**: Current Session  
**Version**: 1.0  
**Maintainer**: Aukett + Heese Development Team
