# Modern Window Template - Aukett + Heese Revit Add-In 2024

## Overview

The Modern Window Template provides a complete, scalable foundation for all application windows in the Aukett + Heese Revit Add-In. It includes comprehensive styling, layout management, and functionality that can be easily extended for new features.

## Features

### 🎨 **Visual Design System**
- **Consistent Branding**: Aukett + Heese color palette and typography
- **Modern UI**: Gradient headers, card-based layouts, smooth animations
- **Responsive Design**: Adapts to different content types and screen sizes
- **Professional Styling**: Drop shadows, rounded corners, hover effects

### 🔧 **Built-in Functionality**
- **Search & Filter**: Real-time search with configurable filters
- **Content Management**: Loading states, error handling, empty states
- **Status System**: Dynamic status indicators and messaging
- **Event System**: Comprehensive event handling for all interactions

### 📱 **Scalable Architecture**
- **Modular Components**: Reusable styles and templates
- **Configurable UI**: Show/hide elements based on requirements
- **Extensible Design**: Easy to add new features while maintaining consistency
- **Data Binding**: Full MVVM support with property change notifications

## File Structure

```
Resources/templates/
├── ModernWindow/                      # Modern Window Template
│   ├── ModernWindowTemplate.xaml      # Main XAML template
│   └── ModernWindowTemplate.xaml.cs   # Code-behind with base functionality
├── ExampleWindow.xaml                 # Example implementation
├── ExampleWindow.xaml.cs              # Example code-behind
└── README.md                          # This documentation
```

## Quick Start Guide

### 1. Create a New Window

**Note**: The Modern Window Template files are located in the `ModernWindow/` subfolder:
- `ModernWindow/ModernWindowTemplate.xaml` - Main XAML template
- `ModernWindow/ModernWindowTemplate.xaml.cs` - Code-behind with base functionality

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
