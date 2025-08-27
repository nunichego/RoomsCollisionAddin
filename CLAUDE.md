# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Revit 2024 add-in** built with **.NET Framework 4.8** that analyzes room-wall collisions and synchronizes parameters. The add-in creates a ribbon panel called "RoomDataSync" with buttons for running analysis, settings, and help.

## Build and Development Commands

### Building the Project
**IMPORTANT: Use `dotnet` command, not `msbuild`**
```bash
dotnet build RoomsManagerAddin.csproj --configuration Debug
dotnet build RoomsManagerAddin.csproj --configuration Release
```

### Installation/Deployment

**IMPORTANT: Use PowerShell commands for deployment, not Install.bat (which may deploy old versions)**

#### Proper Deployment Process
1. Build the project first: `dotnet build RoomsManagerAddin.csproj --configuration Debug`
2. Deploy using PowerShell commands:

```powershell
# Copy the newly built DLL
powershell -Command "Copy-Item 'bin\\Debug\\RoomsManagerAddin.dll' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"

# Copy the addin manifest
powershell -Command "Copy-Item 'RoomsManagerAddin.addin' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"

# Verify deployment
powershell -Command "Get-ChildItem 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\' | Where-Object Name -like '*RoomsManager*'"
```

#### Alternative Manual Deployment
If the above commands don't work in your environment, use explicit paths:
```powershell
powershell -Command "Copy-Item 'bin\\Debug\\RoomsManagerAddin.dll' 'C:\\Users\\dmitr\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"
powershell -Command "Copy-Item 'RoomsManagerAddin.addin' 'C:\\Users\\dmitr\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"
```

#### Why Not Install.bat?
The `Install.bat` file may sometimes deploy cached or old versions. Always use the PowerShell commands above for reliable deployment of the latest build.

#### After Deployment
- Restart Revit 2024 to load the updated add-in
- Check the "Add-ins" tab for the "RoomDataSync" panel

## Architecture

### Service-Oriented Design
The application uses a service-oriented architecture with dependency injection patterns:

- **Controllers**: `RoomWallAnalysisController` orchestrates UI logic and coordinates between services
- **Services**: Specialized services handle specific domains (geometry, collision analysis, parameter updates, etc.)
- **Commands**: Revit IExternalCommand implementations that serve as entry points
- **Models**: Data transfer objects and configuration classes

### Key Services
- `CollisionAnalysisService`: Core collision detection between rooms and walls
- `RoomFilterService`: Advanced filtering system with complex rule configurations
- `GeometryService`: Revit geometry calculations and spatial analysis
- `ParameterUpdateService`: Updates Revit element parameters with analysis results
- `ElementCollectorService`: Collects rooms and walls from Revit documents

### UI Pattern
Uses **WPF with XAML** for modern Windows-style interfaces. Templates are provided in `Resources/templates/` for consistent styling across dialogs.

### Filter System
Advanced filtering system allows complex room filtering with:
- **Filter Rules**: Parameter-based conditions (equals, greater than, contains, etc.)
- **Filter Sets**: Logical groupings with AND/OR operators  
- **Nested Logic**: Filter sets can contain other filter sets for complex conditions

## Revit Integration

### Add-in Registration
- Main application class: `App.cs` (implements `IExternalApplication`)
- Manifest file: `RoomsManagerAddin.addin`
- Primary command: `RoomDataSyncCommand.cs`

### Revit API Usage
- **Document access**: Commands receive `ExternalCommandData` with active document
- **Element collection**: Uses filtered element collectors for rooms and walls
- **Transaction management**: All element modifications wrapped in transactions
- **Parameter access**: Reads/writes Revit built-in and shared parameters

### Target Revit Version
- **Revit 2024** (references in project file point to `C:\Program Files\Autodesk\Revit 2024\`)
- Uses RevitAPI.dll and RevitAPIUI.dll

## Key Workflows

### Main Analysis Flow
1. User clicks "Rooms-Walls" button in ribbon
2. `RoomDataSyncCommand` opens `RoomWallAnalysisWindow`
3. Controller loads rooms and walls data
4. User applies filters and configures analysis
5. `CollisionAnalysisService` performs spatial analysis
6. Results displayed with collision counts and updated parameters

### Filter Configuration
1. `RoomFilterService` discovers available room parameters
2. UI allows building complex filter rules with logical operators
3. Filters validated before application
4. Real-time preview of matching room count

## Development Notes

- **WPF UI**: Modern styling applied using templates in `Resources/templates/`
- **Error Handling**: Comprehensive try-catch blocks with user-friendly error dialogs
- **Progress Reporting**: Callback-based progress reporting for long-running operations
- **Logging**: Built-in logging service for debugging and audit trails
- **Resource Management**: Embedded icons and templates as embedded resources

## Documentation Requirements

**IMPORTANT**: Always update project documentation files when making changes:

### Required Documentation Updates
- **`Resources/documents/history.md`** - Update after significant features, optimizations, or architectural changes
- **`Resources/documents/errors_history.md`** - Document all errors encountered and their solutions

### When to Update
- After implementing new features or major functionality
- After fixing bugs or resolving errors (document the error and solution)
- After performance optimizations or architectural refactoring
- After completing development phases or milestones
- When encountering and solving Revit API issues

### Documentation Format
- Include dates, problem descriptions, solutions implemented
- Document performance improvements with specific metrics
- Record lessons learned and prevention strategies
- Update "Current Status" and "Next Steps" sections

## Task Clarification Protocol

**IMPORTANT**: Before implementing any change request, always ask 3 clarifying questions to ensure proper understanding:

### Required Questions Format
When the user requests changes, respond with exactly 3 questions to clarify:
1. **Scope Question** - What specific components/files should be modified?
2. **Implementation Question** - How should the change be implemented technically?
3. **Impact Question** - What are the expected outcomes or side effects?

### Examples
- "Which specific service/component should handle this new functionality?"
- "Should this be implemented as a new method or modify existing logic?"
- "How should this change affect the existing UI/workflow?"
- "What performance considerations should be taken into account?"
- "Should this be backwards compatible with existing functionality?"

### Purpose
- Prevent misunderstandings and incorrect implementations
- Ensure changes align with user expectations
- Identify potential conflicts or dependencies early
- Maintain code quality and architectural consistency