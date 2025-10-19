# RoomsManagerAddin - Architecture Overview

## Executive Summary

RoomsManagerAddin is a Revit 2024 add-in built with .NET Framework 4.8 that analyzes room-wall and room-floor relationships using Revit's native APIs. The application uses a **layered architecture** with **dependency injection** for maintainability and testability.

## Architecture Layers

### 1. Presentation Layer (`src/Presentation/`)

**Responsibility**: User interface and user interaction

- **Windows/**: WPF windows and dialogs
  - `RoomWallAnalysisWindow` - Main analysis interface
  - `ModernProgressWindow` - Progress tracking during analysis
  - Various configuration dialogs

- **Controls/**: Reusable WPF user controls
  - `FilterRulesPanel` - Complex filtering UI

**Key Characteristics**:
- WPF with XAML for modern UI
- MVVM-like pattern (Window binds to Controllers)
- Minimal business logic (delegates to Controllers)

### 2. Application Layer (`src/Application/`)

**Responsibility**: Orchestration and workflow coordination

- **Commands/**: Revit IExternalCommand implementations
  - `RoomDataSyncCommand` - Main entry point for analysis
  - `SettingsCommand`, `HelpCommand` - Auxiliary commands

- **Controllers/**: Business workflow orchestration
  - `RoomWallAnalysisController` - Orchestrates room-wall analysis workflow
  - `GenericElementController` - Generic element operations

**Key Characteristics**:
- No direct Revit API calls (delegates to services)
- Handles user interaction workflow
- Converts between UI models and domain models
- Manages transactions and error handling

### 3. Domain Layer (`src/Domain/`)

**Responsibility**: Core business logic and domain rules

#### Services (`src/Domain/Services/`)

**Analysis Services** (`Analysis/`):
- `CollisionAnalysisService` - Delegates to specialized analysis services
- `WallBoundaryAnalysisService` - Room-Wall boundary detection using Revit Room Boundary API
- `FloorBoundaryAnalysisService` - Room-Floor intersection using solid geometry

**Filtering Services** (`Filtering/`):
- `RoomFilterService` - Advanced room filtering with complex rule sets
- `GenericElementFilterService` - Generic element filtering
- `RoomParameterDiscoveryService` - Discovers available room parameters
- `ElementParameterDiscoveryService` - Discovers element parameters

**Processing Services** (`Processing/`):
- `RoomProcessingService` - Room-specific processing logic
- `WallProcessingService` - Wall-specific processing logic
- `ParameterMappingExecutionService` - Executes parameter value mappings

**Mapping Services** (`Mapping/`):
- `ParameterMappingService` - Configures parameter mappings between elements

#### Models (`src/Domain/Models/`)

**Shared Models** (`Shared/`):
- `RoomItem`, `WallItem`, `FloorItem`, `ElementItem` - UI-friendly data transfer objects
- `InitialDataResult` - Aggregates initial data for UI
- `ProgressInfo` - Progress reporting data

**Filtering Models** (`Filtering/`):
- `RoomFilterConfiguration` - Complete filter configuration
- `FilterSet` - Logical grouping of filter rules (AND/OR)
- `RoomFilterRule` - Individual filter condition
- `FilterOperator`, `LogicalOperator` - Enums for filter logic
- `ParameterInfo`, `ParameterDataType` - Parameter metadata

**Analysis Models** (`Analysis/`):
- `RoomCollisionResult` - Results of room boundary/collision analysis
- `RoomAnalysisResult` - Detailed room analysis results
- `CurveGroups`, `RoomPreviewResult` - Geometry analysis results

**Configuration Models** (`Configuration/`):
- `AppSettings` - Application configuration
- `ParameterMappingConfiguration` - Parameter mapping configuration

**Key Characteristics**:
- No dependencies on infrastructure or presentation
- Pure business logic and domain rules
- Interfaces for all services (enables DI and testing)

### 4. Infrastructure Layer (`src/Infrastructure/`)

**Responsibility**: External concerns and technical implementations

**RevitApi** (`RevitApi/`):
- `ElementCollectorService` - Collects elements from Revit document
- `GeometryService` - Geometry calculations and solid operations
- `ParameterUpdateService` - Updates Revit element parameters

**Logging** (`Logging/`):
- `LoggingService` - File-based logging with timestamps

**Configuration** (`Configuration/`):
- `ConfigurationService` - Application configuration management

**Progress** (`Progress/`):
- `ProgressReporter` - Progress reporting with callbacks
- `ProgressService` - Progress tracking and timing

**Key Characteristics**:
- All Revit API calls isolated here
- Implements infrastructure interfaces
- Throws `RevitApiException` for Revit API errors

### 5. Core Layer (`src/Core/`)

**Responsibility**: Cross-cutting concerns and shared utilities

**DependencyInjection** (`DependencyInjection/`):
- `ServiceContainer` - Lightweight DI container
- `ServiceDescriptor`, `ServiceLifetime` - DI configuration

**Exceptions** (`Exceptions/`):
- `RoomsManagerException` - Base exception with user/technical messages
- `RevitApiException` - Revit API operation failures
- `CollisionAnalysisException` - Analysis errors
- `FilterValidationException` - Filter configuration errors

**ErrorHandling** (`ErrorHandling/`):
- `GlobalErrorHandler` - Centralized error handling and user messages

**Extensions** (`Extensions/`):
- Extension methods (currently minimal)

**Key Characteristics**:
- No dependencies on other layers
- Reusable across any .NET application
- Foundational utilities

## Dependency Flow

```
┌─────────────────────────────────────┐
│      Presentation Layer             │
│  (Windows, Controls, Views)         │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│     Application Layer               │
│  (Commands, Controllers)            │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│       Domain Layer                  │
│  (Services, Models)                 │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│    Infrastructure Layer             │
│  (RevitApi, Logging, Config)        │
└─────────────────────────────────────┘
               │
               ▼
         Core Layer
  (DI, Exceptions, Extensions)
```

**Key Principles**:
- Upper layers depend on lower layers
- Domain layer is independent (no infrastructure dependencies)
- Infrastructure implements domain interfaces
- Core layer has no dependencies

## Dependency Injection

### Service Lifetime Strategies

**Singleton** (Created once, shared):
- `ILoggingService` - Single log file throughout session
- `IConfigurationService` - Single configuration throughout session

**Transient** (Created per request):
- All Domain services - Document-dependent, stateless
- All RevitApi services - Document-dependent
- Controllers - Per-command execution

### Registration (App.cs)

```csharp
private void ConfigureServices(IServiceContainer services)
{
    // Infrastructure (Singleton)
    services.AddSingleton<ILoggingService, LoggingService>();
    services.AddSingleton<IConfigurationService, ConfigurationService>();

    // Infrastructure (Transient - Document-dependent)
    services.AddTransient<IElementCollectorService, ElementCollectorService>();
    services.AddTransient<GeometryService>(c => new GeometryService());

    // Domain - Analysis
    services.AddTransient<ICollisionAnalysisService, CollisionAnalysisService>();
    services.AddTransient<IWallBoundaryAnalysisService, WallBoundaryAnalysisService>();
    services.AddTransient<IFloorBoundaryAnalysisService, FloorBoundaryAnalysisService>();

    // Domain - Filtering
    services.AddTransient<IRoomFilterService, RoomFilterService>();

    // Domain - Processing & Mapping
    services.AddTransient<IRoomProcessingService, RoomProcessingService>();
    services.AddTransient<IParameterMappingService, ParameterMappingService>();
}
```

### Usage in Commands

```csharp
public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
{
    var serviceContainer = App.ServiceContainer;
    var elementCollector = serviceContainer.Resolve<IElementCollectorService>();
    var collisionService = serviceContainer.Resolve<ICollisionAnalysisService>();
    var loggingService = serviceContainer.Resolve<ILoggingService>();

    // Use services...
}
```

## Error Handling Strategy

### Exception Hierarchy

```
Exception
 └─ RoomsManagerException (Base)
     ├─ RevitApiException (Infrastructure failures)
     ├─ CollisionAnalysisException (Analysis errors)
     └─ FilterValidationException (Filter validation)
```

### Error Flow

1. **Services** throw specific exceptions (`RevitApiException`, `CollisionAnalysisException`, etc.)
2. **Controllers** catch `RoomsManagerException` and display user-friendly messages
3. **GlobalErrorHandler** provides centralized error handling and logging

### User Message Strategy

Each exception has:
- `UserMessage` - User-friendly explanation
- `TechnicalDetails` - Full stack trace for logging
- `ShowToUser` - Whether to display dialog

Example:
```csharp
catch (RevitApiException rme)
{
    _loggingService.WriteToLog($"ERROR: {rme.Message}");
    _loggingService.WriteToLog($"Technical: {rme.TechnicalDetails}");

    if (rme.ShowToUser)
    {
        TaskDialog.Show("Error", rme.UserMessage);
    }
}
```

## Key Design Decisions

### 1. Room Boundary API vs Solid Intersection

**Walls**: Uses Revit's Room Boundary API
- **Reason**: Native, fast, accurate for walls
- **Performance**: ~100x faster than solid intersection
- **Method**: `Room.GetBoundarySegments()`

**Floors**: Uses solid intersection
- **Reason**: Floor boundaries not well-supported by Room Boundary API
- **Performance**: Slower but necessary for accuracy
- **Method**: `BooleanOperationsUtils.ExecuteBooleanOperation()`

### 2. Lightweight DI Container

**Why custom instead of third-party?**
- Revit add-ins should minimize dependencies
- Simple requirements (no property injection, scopes, etc.)
- Full control over service lifetime
- ~150 lines of code vs megabytes of dependencies

### 3. Parameter Mapping Architecture

Parameters can be mapped:
- **Room → Wall**: Room properties → Wall parameters
- **Wall → Room**: Wall properties → Room parameters
- **Bidirectional**: Both directions simultaneously

**Execution Order**:
1. Room → Element mappings (batch)
2. Element → Room mappings (batch)

This ensures consistency when both directions are configured.

### 4. Filter System Design

**Composable Filters**:
- `FilterRule`: Single condition (Parameter Operator Value)
- `FilterSet`: Logical group of rules/sets (AND/OR)
- `FilterConfiguration`: Named filter with root FilterSet

**Supports**:
- Nested logic: `(A AND B) OR (C AND D)`
- Multiple parameter types: String, Integer, Double, YesNo, ElementId
- Rich operators: Equals, NotEquals, Contains, GreaterThan, LessThan, HasValue, HasNoValue

**Validation**:
- Type-appropriate operators
- Value format validation
- Recursive validation of nested sets

## Performance Characteristics

| Operation | Typical Time | Notes |
|-----------|-------------|-------|
| Load 50 rooms | < 100ms | Element collection |
| Load 200 walls | < 200ms | Element collection |
| Analyze 50 rooms (walls) | 2-5 seconds | Room Boundary API |
| Analyze 50 rooms (floors) | 10-30 seconds | Solid intersection |
| Complex filter (1000 rooms) | < 500ms | In-memory evaluation |
| Parameter mapping (50 rooms) | 1-2 seconds | Revit transaction |

## Future Extensibility

### Adding New Analysis Types

1. Create interface in `Domain.Services.Analysis.I[Type]AnalysisService`
2. Implement service in `Domain.Services.Analysis.[Type]AnalysisService`
3. Register in `App.ConfigureServices()`
4. Update `CollisionAnalysisService` to delegate

### Adding New Filter Types

1. Create model in `Domain.Models.Filtering.`
2. Implement `IFilterItem` interface
3. Add operator support in `FilterOperator` enum
4. Update `RoomFilterService.ApplyFilter()` logic

### Adding New Parameter Mappings

1. Add to `ParameterMappingConfiguration` model
2. Update `ParameterMappingExecutionService.Execute()` logic
3. Update UI in `ParameterMappingWindow`

## Testing Strategy

### Unit Tests (Future - Phase 6)

- **Core Layer**: DI container, exceptions
- **Domain Services**: Business logic with mocked dependencies
- **Filtering**: Rule validation, operator logic

### Integration Tests (Future - Phase 6)

- **RevitApi Services**: Requires Revit test framework
- **End-to-end**: Full workflows with test documents

### Manual Testing

- Small model (10 rooms, 50 walls): < 5 seconds
- Medium model (50 rooms, 200 walls): < 30 seconds
- Large model (200 rooms, 1000 walls): < 2 minutes

## Deployment

See `CLAUDE.md` for detailed build and deployment instructions.

**Quick Deploy**:
```powershell
dotnet build RoomsManagerAddin.csproj --configuration Debug
Copy-Item 'bin\Debug\net48\RoomsManagerAddin.dll' "$env:APPDATA\Autodesk\Revit\Addins\2024\"
Copy-Item 'RoomsManagerAddin.addin' "$env:APPDATA\Autodesk\Revit\Addins\2024\"
```

**Restart Revit** to load updated add-in.

## Documentation Structure

```
docs/
├── architecture/
│   ├── overview.md (this file)
│   └── dependency-injection.md
├── api/
│   └── services.md
└── guides/
    ├── adding-features.md
    ├── debugging.md
    └── testing.md
```

## Version History

- **v1.0** (2025-10-18): Initial release with Room-Wall analysis
- **v1.1** (2025-10-18): Added Floor analysis
- **v2.0** (2025-10-19): Layered architecture refactoring (Phases 1-4 complete)
  - Dependency injection
  - Error handling standardization
  - Clean separation of concerns

---

**Last Updated**: 2025-10-19
**Refactoring Status**: Phase 4 Complete (~67% done)
