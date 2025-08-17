# Rooms Manager Add-in

A Revit 2024 add-in for analyzing room volumes and detecting element collisions within room boundaries.

## Features

- **Room Volume Analysis**: Analyze room geometry, area, and volume
- **Collision Detection**: Detect elements that intersect with room boundaries
- **Comprehensive Reporting**: Detailed analysis results with collision counts
- **Modern Architecture**: Built with .NET Framework 4.8 and modern patterns

## Installation

1. Build the project using Visual Studio or `dotnet build`
2. The add-in will be automatically deployed to Revit's add-ins folder
3. Restart Revit 2024
4. Look for the "Rooms Manager" tab in the ribbon

## Usage

1. Open a Revit project with rooms
2. Click the "Room Volumes" button in the "Rooms Manager" panel
3. The add-in will analyze all rooms and show results in a dialog

## Project Structure

```
RoomsManagerAddin/
├── App.cs                          # Main application class
├── Commands/
│   ├── BaseCommand.cs              # Base command class
│   └── RoomVolumesCommand.cs       # Room volumes analysis command
├── Models/
│   └── AppSettings.cs              # Application settings model
├── Services/
│   ├── IConfigurationService.cs    # Configuration service interface
│   └── ConfigurationService.cs     # Configuration service implementation
├── Properties/
│   └── AssemblyInfo.cs             # Assembly metadata
└── Resources/
    └── icons/                      # Ribbon button icons
```

## Development

This project uses:
- **.NET Framework 4.8** for Revit 2024 compatibility
- **WPF** for user interface components
- **Microsoft.Extensions.DependencyInjection** for service management
- **Microsoft.Extensions.Logging** for comprehensive logging
- **Newtonsoft.Json** for configuration management

## Building

```bash
dotnet build RoomsManagerAddin.csproj
```

The add-in will be automatically deployed to `%APPDATA%\Autodesk\Revit\Addins\2024\` during build.
