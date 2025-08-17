# Revit Add-in Template - Aukett + Heese

## Overview

The Revit Add-in Template provides a comprehensive, modular foundation for developing Revit add-ins with modern architecture patterns, dependency injection, comprehensive logging, and reusable UI templates. This template is designed to accelerate development and ensure consistency across multiple add-in projects.

## 🏗️ **Architecture Highlights**

### **Modular Design**
- **Command Separation**: Each command is a separate class that can be easily migrated to other plugins
- **Service Layer**: Business logic separated into injectable services
- **Model Layer**: Clean data models with property change notifications
- **UI Templates**: Reusable WPF templates for consistent user interfaces

### **Modern Patterns**
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service management
- **Logging**: Comprehensive logging with Microsoft.Extensions.Logging
- **Configuration**: JSON-based configuration with validation and backup
- **MVVM**: Model-View-ViewModel pattern for WPF windows

### **Extensibility**
- **Plugin Architecture**: Commands can be easily moved between add-ins
- **Template System**: Reusable UI templates for rapid development
- **Service Contracts**: Interface-based design for easy testing and extension

## 📁 **Project Structure**

```
RevitAddinTemplate/
├── App.cs                              # Main application class (IExternalApplication)
├── RevitAddinTemplate.csproj           # Project file with all dependencies
├── RevitAddinTemplate.addin            # Revit add-in manifest
├── Properties/
│   └── AssemblyInfo.cs                 # Assembly metadata
├── Commands/                           # Revit commands (modular)
│   ├── BaseCommand.cs                  # Base command with common functionality
│   ├── AboutCommand.cs                 # Example command implementation
│   ├── AdminPanelCommand.cs            # Command to open admin panel
│   ├── ContentFilesCommand.cs          # Content management command
│   ├── DynamoScriptsCommand.cs         # Dynamo scripts command
│   ├── HelpCommand.cs                  # Help command
│   └── SettingsCommand.cs              # Settings command
├── Models/                             # Data models
│   ├── AppSettings.cs                  # Application settings model
│   ├── AdminConfig.cs                  # Admin configuration model
│   ├── MasterConfig.cs                 # Master configuration model
│   ├── ContentItem.cs                  # Content item model
│   ├── ContentLibraryModels.cs         # Content library models
│   └── DynamoScript.cs                 # Dynamo script model
├── Services/                           # Business logic services
│   ├── IConfigurationService.cs        # Configuration service interface
│   ├── ConfigurationService.cs         # JSON-based configuration service
│   ├── IFileService.cs                 # File service interface
│   ├── FileService.cs                  # File operations service
│   ├── IEncryptionService.cs           # Encryption service interface
│   ├── EncryptionService.cs            # AES encryption service
│   ├── IContentManager.cs              # Content management interface
│   ├── ContentManager.cs               # Content management service
│   ├── IDynamoScriptsManager.cs        # Dynamo scripts interface
│   ├── DynamoScriptsManager.cs         # Dynamo scripts service
│   ├── IAdminConfigurationService.cs   # Admin config interface
│   └── AdminConfigurationService.cs    # Admin configuration service
├── Windows/                            # WPF windows
│   ├── AdminPanel/                     # Admin panel windows
│   │   ├── AdminPanelWindow.xaml       # Main admin panel
│   │   ├── AdminPanelWindow.xaml.cs    # Admin panel code-behind
│   │   ├── AdminPasswordDialog.xaml    # Password dialog
│   │   ├── AdminPasswordDialog.xaml.cs # Password dialog code-behind
│   │   └── ...                         # Other admin windows
│   ├── ContentBrowser/                 # Content browser windows
│   │   ├── ContentBrowserWindow.xaml   # Content browser
│   │   └── ContentBrowserWindow.xaml.cs
│   └── DynamoScripts/                  # Dynamo scripts windows
│       ├── DynamoScriptsWindow.xaml    # Dynamo scripts manager
│       └── DynamoScriptsWindow.xaml.cs
├── Utils/                              # Utility classes
│   └── WipCommand.cs                   # Work-in-progress command
├── Data/                               # Configuration data
│   ├── content-libraries.json          # Content libraries configuration
│   └── dynamo-scripts.json             # Dynamo scripts configuration
├── Resources/                          # Application resources
│   ├── icons/                          # Command icons
│   │   ├── administrator-16_96dpi.png  # Admin panel icon
│   │   ├── administrator-32_96dpi.png  # Admin panel icon (large)
│   │   ├── content-library-16_96dpi.png # Content library icon
│   │   ├── content-library-32_96dpi.png # Content library icon (large)
│   │   ├── dynamo-scripts-16_96dpi.png # Dynamo scripts icon
│   │   ├── dynamo-scripts-32_96dpi.png # Dynamo scripts icon (large)
│   │   ├── help-16_96dpi.png          # Help icon
│   │   ├── help-32_96dpi.png          # Help icon (large)
│   │   ├── settings-16_96dpi.png      # Settings icon
│   │   ├── settings-32_96dpi.png      # Settings icon (large)
│   │   ├── icon_info_16_96dpi.png     # Info icon
│   │   └── icon_info_32_96dpi.png     # Info icon (large)
│   └── templates/                      # WPF templates
│       ├── ModernWindow/               # Modern window template
│       │   ├── ModernWindowTemplate.xaml
│       │   ├── ModernWindowTemplate.xaml.cs
│       │   ├── ExampleWindow.xaml
│       │   ├── ExampleWindow.xaml.cs
│       │   └── README.md
│       ├── SimpleWindow/               # Simple window template
│       │   ├── SimpleWindowTemplate.xaml
│       │   ├── SimpleWindowTemplate.xaml.cs
│       │   ├── ExampleSimpleWindow.xaml
│       │   ├── ExampleSimpleWindow.xaml.cs
│       │   └── README.md
│       └── README.md                   # Templates documentation
├── Deployment/                         # Deployment files
│   ├── RevitAddinTemplate.dll          # Compiled assembly
│   ├── INSTALL.md                      # Installation instructions
│   └── Uninstall.bat                   # Uninstall script
├── INSTALL.bat                         # Installation script
├── LICENSE                             # License file
└── README.md                           # This documentation
```

## 🚀 **Quick Start Guide**

### **1. Create New Add-in Project**

1. **Copy Template**: Copy the entire `RevitAddinTemplate` folder to your new project location
2. **Rename Project**: Update project name, namespace, and assembly references
3. **Update GUIDs**: Generate new GUIDs for the add-in manifest and assembly
4. **Customize Branding**: Update company name, product name, and descriptions

### **2. Configure Project Settings**

```xml
<!-- Update in RevitAddinTemplate.csproj -->
<PropertyGroup>
    <AssemblyName>YourAddinName</AssemblyName>
    <RootNamespace>YourAddinName</RootNamespace>
</PropertyGroup>
```

```xml
<!-- Update in RevitAddinTemplate.addin -->
<AddIn Type="Application">
    <Name>YourAddinName</Name>
    <Assembly>YourAddinName.dll</Assembly>
    <FullClassName>YourAddinName.App</FullClassName>
    <ClientId>{YOUR-NEW-GUID-HERE}</ClientId>
    <VendorId>YOUR_VENDOR_ID</VendorId>
    <VendorDescription>Your Company Name</VendorDescription>
</AddIn>
```

### **3. Add Custom Commands**

```csharp
// Create new command in Commands/ folder
[Transaction(TransactionMode.Manual)]
public class MyCustomCommand : BaseCommand
{
    protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            // Your command logic here
            ShowInfo("Success", "Command executed successfully");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = $"Error in MyCustomCommand: {ex.Message}";
            return Result.Failed;
        }
    }
}
```

### **4. Register Command in Add-in Manifest**

```xml
<AddIn Type="Command">
    <Name>My Custom Command</Name>
    <Assembly>YourAddinName.dll</Assembly>
    <FullClassName>YourAddinName.Commands.MyCustomCommand</FullClassName>
    <ClientId>{YOUR-NEW-GUID-HERE}</ClientId>
    <VendorId>YOUR_VENDOR_ID</VendorId>
    <Text>My Custom Command</Text>
    <Description>Description of your custom command</Description>
    <Discipline>Any</Discipline>
    <VisibilityMode>AlwaysVisible</VisibilityMode>
</AddIn>
```

### **5. Add Command to Ribbon**

```csharp
// In App.cs, add to CreateRibbonInterface method
AddCommandToPanel(mainPanel, "MyCustomCommand", "My Command", "Description", "my-icon-32_96dpi.png");
```

## 🔧 **Configuration System**

### **Application Settings**
The template includes a comprehensive configuration system:

```csharp
// Get configuration service
var configService = ServiceProvider.GetService<IConfigurationService>();

// Load settings
var settings = configService.GetAppSettings();

// Save settings
settings.ShowWelcomeMessage = false;
configService.SaveAppSettings(settings);
```

### **Configuration Files**
- **app-settings.json**: Application settings
- **admin-config.json**: Admin panel configuration
- **master-config.json**: Master configuration for multi-user deployment

### **Configuration Features**
- **Automatic Backup**: Configurations are backed up before saving
- **Validation**: Configuration validation before saving
- **Default Values**: Automatic creation of default configurations
- **Async Support**: Async loading and saving operations

## 🎨 **UI Template System**

### **Modern Window Template**
For content-rich windows with search, filtering, and grid layouts:

```csharp
// Inherit from ModernWindowTemplate
public partial class MyContentWindow : ModernWindowTemplate
{
    public MyContentWindow()
    {
        InitializeComponent();
        HeaderTitle = "My Content";
        HeaderSubtitle = "Manage your content";
    }

    protected override void LoadContent()
    {
        // Load your content here
        Items = LoadMyItems();
    }
}
```

### **Simple Window Template**
For settings and configuration windows:

```csharp
// Inherit from SimpleWindowTemplate
public partial class MySettingsWindow : SimpleWindowTemplate
{
    public MySettingsWindow()
    {
        InitializeComponent();
        WindowTitle = "My Settings";
    }

    protected override void LoadSettings()
    {
        // Load settings into UI
    }

    protected override void SaveSettings()
    {
        // Save settings from UI
    }
}
```

## 🔌 **Modular Command System**

### **Command Migration**
Commands can be easily moved between add-ins:

1. **Copy Command Class**: Copy the command file to the new project
2. **Update Namespace**: Change the namespace to match the new project
3. **Register in Manifest**: Add the command to the new add-in manifest
4. **Add to Ribbon**: Register the command in the ribbon interface

### **Command Features**
- **Base Command**: Common functionality and error handling
- **Logging**: Automatic logging of command execution
- **Error Handling**: Consistent error handling and user feedback
- **Transaction Support**: Built-in transaction management
- **Document Validation**: Automatic document validation

## 🛠️ **Service Layer**

### **Available Services**
- **IConfigurationService**: JSON-based configuration management
- **IFileService**: File operations and management
- **IEncryptionService**: AES encryption for sensitive data
- **IContentManager**: Content library management
- **IDynamoScriptsManager**: Dynamo scripts management
- **IAdminConfigurationService**: Admin panel configuration

### **Adding Custom Services**
```csharp
// Create service interface
public interface IMyCustomService
{
    void DoSomething();
}

// Create service implementation
public class MyCustomService : IMyCustomService
{
    public void DoSomething()
    {
        // Implementation
    }
}

// Register in App.cs
services.AddSingleton<IMyCustomService, MyCustomService>();
```

## 📦 **Deployment**

### **Build Configuration**
The project includes automatic deployment to Revit's add-in folder:

```xml
<!-- Post-build event in .csproj -->
<PostBuildEvent>
    if not exist "$(APPDATA)\Autodesk\Revit\Addins\2024" mkdir "$(APPDATA)\Autodesk\Revit\Addins\2024"
    copy "$(TargetPath)" "$(APPDATA)\Autodesk\Revit\Addins\2024\"
    copy "$(ProjectDir)*.addin" "$(APPDATA)\Autodesk\Revit\Addins\2024\"
</PostBuildEvent>
```

### **Installation Scripts**
- **INSTALL.bat**: Automated installation script
- **Uninstall.bat**: Clean uninstallation script
- **INSTALL.md**: Manual installation instructions

## 🔍 **Logging System**

### **Logging Configuration**
```csharp
// Logging is configured in App.cs
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});
```

### **Using Logger**
```csharp
// In any command or service
Logger?.LogInformation("Operation completed successfully");
Logger?.LogWarning("Something unexpected happened");
Logger?.LogError(ex, "An error occurred");
```

## 🧪 **Testing**

### **Unit Testing**
The modular architecture supports easy unit testing:

```csharp
// Test configuration service
[Test]
public void TestConfigurationService()
{
    var configService = new ConfigurationService(mockLogger);
    var settings = configService.GetAppSettings();
    Assert.IsNotNull(settings);
}
```

### **Integration Testing**
Test commands with mock Revit environment:

```csharp
[Test]
public void TestCommandExecution()
{
    var command = new MyCustomCommand();
    var result = command.Execute(mockCommandData, ref message, mockElements);
    Assert.AreEqual(Result.Succeeded, result);
}
```

## 📚 **Best Practices**

### **1. Command Development**
- Always inherit from `BaseCommand`
- Use proper transaction modes
- Implement comprehensive error handling
- Log all operations
- Validate document state

### **2. Service Development**
- Use interfaces for all services
- Implement proper dependency injection
- Handle exceptions gracefully
- Use async operations when appropriate
- Validate inputs and outputs

### **3. UI Development**
- Use the provided templates
- Follow MVVM pattern
- Implement proper data binding
- Handle UI thread operations correctly
- Provide user feedback

### **4. Configuration Management**
- Use the configuration service
- Validate configurations before saving
- Provide default values
- Backup configurations before changes
- Use async operations for large configurations

## 🔄 **Migration Guide**

### **From Existing Add-ins**
1. **Copy Template Structure**: Use the template as a foundation
2. **Migrate Commands**: Move existing commands to the new structure
3. **Update Services**: Refactor business logic into services
4. **Adopt Templates**: Use the UI templates for consistency
5. **Update Configuration**: Migrate to the new configuration system

### **From Legacy Projects**
1. **Update Dependencies**: Use modern NuGet packages
2. **Implement Logging**: Add comprehensive logging
3. **Use Dependency Injection**: Refactor to use DI container
4. **Adopt Async Patterns**: Use async/await where appropriate
5. **Implement Error Handling**: Add proper error handling

## 🚀 **Future Enhancements**

### **Planned Features**
- **Plugin System**: Dynamic plugin loading
- **Theme System**: Dark/light mode support
- **Internationalization**: Multi-language support
- **Advanced Logging**: Structured logging with correlation IDs
- **Performance Monitoring**: Built-in performance metrics
- **Cloud Integration**: Cloud-based configuration and content

### **Contribution Guidelines**
- Follow existing code style
- Add comprehensive documentation
- Include unit tests for new features
- Test across different Revit versions
- Update this documentation

## 📞 **Support**

For questions or issues with the Revit Add-in Template:
1. Check this documentation first
2. Review the example implementations
3. Consult the development team
4. Create an issue in the project repository

**Last Updated**: Current Session  
**Version**: 1.0  
**Maintainer**: Aukett + Heese Development Team
