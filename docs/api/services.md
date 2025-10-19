# API Services Catalog

## Overview

This document catalogs all services in RoomsManagerAddin, their responsibilities, dependencies, and usage examples.

## Service Categories

### Infrastructure Services

Core technical services for external concerns.

### Domain Services

Business logic and domain-specific operations.

### Application Services

Workflow orchestration and UI coordination.

---

## Infrastructure Services

### ILoggingService

**Location**: `src/Infrastructure/Logging/LoggingService.cs`
**Interface**: `src/Infrastructure/Logging/ILoggingService.cs`
**Lifetime**: Singleton

**Responsibility**: File-based logging with timestamps

**Dependencies**: None

**Methods**:
- `InitializeDebugLogging(IntPtr? ownerWindowHandle)` - Creates log file with SaveFileDialog
- `WriteToLog(string message)` - Writes timestamped message
- `LogInfo(string message)` - Writes [INFO] message
- `LogError(string message)` - Writes [ERROR] message
- `LogWarning(string message)` - Writes [WARNING] message
- `GetDebugLogPath()` - Returns current log file path

**Usage**:
```csharp
var logging = container.Resolve<ILoggingService>();
var logPath = logging.InitializeDebugLogging(revitWindowHandle);
logging.LogInfo("Analysis started");
logging.LogError("Something went wrong");
```

---

### IElementCollectorService

**Location**: `src/Infrastructure/RevitApi/ElementCollectorService.cs`
**Interface**: `src/Infrastructure/RevitApi/IElementCollectorService.cs`
**Lifetime**: Transient

**Responsibility**: Collect elements from Revit document

**Dependencies**: None (requires Document)

**Methods**:
- `GetRooms(Document document)` - Returns all rooms with area > 0
- `GetWalls(Document document)` - Returns all walls with valid wall types
- `GetFloors(Document document)` - Returns all floors with valid floor types

**Exceptions**:
- `ArgumentNullException` - Document is null
- `RevitApiException` - Revit API operation fails

**Usage**:
```csharp
var collector = container.Resolve<IElementCollectorService>();
var rooms = collector.GetRooms(document);
var walls = collector.GetWalls(document);
var floors = collector.GetFloors(document);
```

---

### GeometryService

**Location**: `src/Infrastructure/RevitApi/GeometryService.cs`
**Lifetime**: Transient

**Responsibility**: Geometry calculations and solid operations

**Dependencies**: None

**Methods**:
- `GetElementSolid(Element element)` - Extracts solid geometry from element
- `GetWallSolid(Wall wall)` - Gets solid from wall (handles curtain walls)
- `CreateCurtainWallSolid(Wall wall)` - Creates proxy solid for curtain walls
- `SolidsIntersect(Solid solid1, Solid solid2)` - Checks if two solids intersect

**Exceptions**:
- `ArgumentNullException` - Element/solid is null
- `RevitApiException` - Geometry operation fails

**Usage**:
```csharp
var geometryService = container.Resolve<GeometryService>();
var roomSolid = geometryService.GetElementSolid(room);
var wallSolid = geometryService.GetWallSolid(wall);
bool intersects = geometryService.SolidsIntersect(roomSolid, wallSolid);
```

---

### IConfigurationService

**Location**: `src/Infrastructure/Configuration/ConfigurationService.cs`
**Interface**: `src/Infrastructure/Configuration/IConfigurationService.cs`
**Lifetime**: Singleton

**Responsibility**: Application configuration management

**Dependencies**: None

**Methods**:
- `GetSettings()` - Returns AppSettings object
- `SaveSettings(AppSettings settings)` - Persists settings

**Usage**:
```csharp
var config = container.Resolve<IConfigurationService>();
var settings = config.GetSettings();
settings.CollisionTolerance = 0.01;
config.SaveSettings(settings);
```

---

## Domain Services - Analysis

### ICollisionAnalysisService

**Location**: `src/Domain/Services/Analysis/CollisionAnalysisService.cs`
**Interface**: `src/Domain/Services/Analysis/ICollisionAnalysisService.cs`
**Lifetime**: Transient

**Responsibility**: Orchestrates collision analysis (delegates to specialized services)

**Dependencies**:
- `IWallBoundaryAnalysisService`
- `IFloorBoundaryAnalysisService`

**Methods**:
- `AnalyzeRoomCollisions(Document, rooms, walls, paramMappings, log, progress)` - Analyzes room-wall relationships
- `AnalyzeRoomFloorsCollisions(Document, rooms, floors, paramMappings, log, progress)` - Analyzes room-floor relationships

**Returns**: `List<RoomCollisionResult>`

**Usage**:
```csharp
var collisionService = container.Resolve<ICollisionAnalysisService>();
var results = collisionService.AnalyzeRoomCollisions(
    document,
    rooms,
    walls,
    parameterMappings,
    loggingService.WriteToLog,
    progressReporter);

foreach (var result in results)
{
    Console.WriteLine($"Room {result.RoomNumber}: {result.WallsColliding} walls");
}
```

---

### IWallBoundaryAnalysisService

**Location**: `src/Domain/Services/Analysis/WallBoundaryAnalysisService.cs`
**Interface**: `src/Domain/Services/Analysis/IWallBoundaryAnalysisService.cs`
**Lifetime**: Transient

**Responsibility**: Room-wall boundary analysis using Revit Room Boundary API

**Dependencies**:
- `IParameterMappingExecutionService`

**Methods**:
- `AnalyzeRoomCollisions(...)` - Performs room-wall boundary analysis

**Key Features**:
- Uses Revit's native `Room.GetBoundarySegments()` API
- ~100x faster than solid intersection
- Executes parameter mappings in transaction
- Batch processing for performance

**Performance**:
- 50 rooms: 2-5 seconds
- 200 rooms: 10-20 seconds

**Usage**:
```csharp
var wallBoundaryService = container.Resolve<IWallBoundaryAnalysisService>();
var results = wallBoundaryService.AnalyzeRoomCollisions(
    document,
    rooms,
    walls,
    parameterMappings,
    loggingService.WriteToLog,
    progressReporter);
```

---

### IFloorBoundaryAnalysisService

**Location**: `src/Domain/Services/Analysis/FloorBoundaryAnalysisService.cs`
**Interface**: `src/Domain/Services/Analysis/IFloorBoundaryAnalysisService.cs`
**Lifetime**: Transient

**Responsibility**: Room-floor collision analysis using solid intersection

**Dependencies**:
- `IParameterMappingExecutionService`
- `GeometryService`
- `IRoomProcessingService`

**Methods**:
- `AnalyzeRoomFloorsCollisions(...)` - Performs room-floor intersection analysis

**Key Features**:
- Uses Boolean solid operations for accurate floor detection
- Creates vertically expanded room solids for better floor matching
- Handles multi-level rooms

**Performance**:
- 50 rooms: 10-30 seconds (slower due to solid operations)

**Usage**:
```csharp
var floorBoundaryService = container.Resolve<IFloorBoundaryAnalysisService>();
var results = floorBoundaryService.AnalyzeRoomFloorsCollisions(
    document,
    rooms,
    floors,
    parameterMappings,
    loggingService.WriteToLog,
    progressReporter);
```

---

## Domain Services - Filtering

### IRoomFilterService

**Location**: `src/Domain/Services/Filtering/RoomFilterService.cs`
**Interface**: `src/Domain/Services/Filtering/IRoomFilterService.cs`
**Lifetime**: Transient (requires Document)

**Responsibility**: Advanced room filtering with complex rule sets

**Dependencies**:
- `Document` (constructor parameter)
- `RoomParameterDiscoveryService`
- `ILoggingService`
- `IElementCollectorService`

**Methods**:
- `GetAvailableParameters()` - Lists all available room parameters
- `CreateFilterRule(paramName, operator, value)` - Creates a single filter rule
- `CreateFilterSet(LogicalOperator)` - Creates AND/OR group
- `CreateFilterConfiguration(name)` - Creates named filter config
- `ApplyFilter(RoomFilterConfiguration)` - Applies filter and returns matching rooms
- `ValidateFilterSet(FilterSet)` - Validates filter configuration
- `CountMatchingRooms(RoomFilterConfiguration)` - Counts matches without full filter
- `GetFilterDescription(RoomFilterConfiguration)` - Human-readable filter description

**Exceptions**:
- `FilterValidationException` - Invalid filter configuration
- `RevitApiException` - Revit API failures

**Usage**:
```csharp
var roomFilter = new RoomFilterService(document, loggingService);

// Get available parameters
var parameters = roomFilter.GetAvailableParameters();

// Create filter: Area > 100 AND Level = "Level 1"
var filterConfig = roomFilter.CreateFilterConfiguration("Large Rooms on Level 1");
var areaRule = roomFilter.CreateFilterRule("Area", FilterOperator.GreaterThan, "100");
var levelRule = roomFilter.CreateFilterRule("Level", FilterOperator.Equals, "Level 1");

filterConfig.RootFilterSet.Items.Add(areaRule);
filterConfig.RootFilterSet.Items.Add(levelRule);

// Apply filter
var matchingRooms = roomFilter.ApplyFilter(filterConfig);
Console.WriteLine($"Found {matchingRooms.Count} matching rooms");
```

---

## Domain Services - Processing & Mapping

### IParameterMappingExecutionService

**Location**: `src/Domain/Services/Processing/ParameterMappingExecutionService.cs`
**Interface**: `src/Domain/Services/Processing/IParameterMappingExecutionService.cs`
**Lifetime**: Transient

**Responsibility**: Execute parameter value mappings between elements

**Dependencies**:
- Logging callback (`Action<string>`)

**Methods**:
- `ValidateAllMappings(mappings)` - Validates mapping configurations
- `ExecuteRoomToElementMappingsBatch(roomElementMap, mappings)` - Batch room→element mappings
- `ExecuteElementToRoomMappings(elementRoomMap, elementMap, mappings)` - Batch element→room mappings
- `SetProgressReporter(ProgressReporter)` - Sets progress callback

**Usage**:
```csharp
var paramMappingService = container.Resolve<IParameterMappingExecutionService>();

// Define mappings
var mappings = new List<ParameterMappingConfiguration>
{
    new ParameterMappingConfiguration
    {
        SourceParameterName = "Room Name",
        TargetParameterName = "Comments",
        Direction = MappingDirection.RoomToElement
    }
};

// Execute mappings
paramMappingService.ExecuteRoomToElementMappingsBatch(
    roomElementRelationships,
    mappings);
```

---

### IParameterMappingService

**Location**: `src/Domain/Services/Mapping/ParameterMappingService.cs`
**Interface**: `src/Domain/Services/Mapping/IParameterMappingService.cs`
**Lifetime**: Transient (requires Document)

**Responsibility**: Configure parameter mappings UI/workflow

**Dependencies**:
- `Document` (constructor parameter)

**Methods**:
- `GetAvailableSourceParameters()` - Lists parameters for source elements
- `GetAvailableTargetParameters()` - Lists parameters for target elements
- `CreateMapping(source, target, direction)` - Creates mapping configuration
- `ValidateMapping(ParameterMappingConfiguration)` - Validates compatibility

**Usage**:
```csharp
var mappingService = new ParameterMappingService(document);

var sourceParams = mappingService.GetAvailableSourceParameters();
var targetParams = mappingService.GetAvailableTargetParameters();

var mapping = mappingService.CreateMapping(
    "Room Name",
    "Comments",
    MappingDirection.RoomToElement);

bool isValid = mappingService.ValidateMapping(mapping);
```

---

## Application Services

### RoomWallAnalysisController

**Location**: `src/Application/Controllers/RoomWallAnalysisController.cs`
**Lifetime**: Created per-command (not in DI container)

**Responsibility**: Orchestrate room-wall analysis workflow

**Dependencies** (Constructor Injection):
- `Document`
- `IElementCollectorService`
- `ICollisionAnalysisService`
- `ILoggingService`
- `RoomFilterService`

**Methods**:
- `LoadInitialData()` - Loads rooms, walls, floors for UI
- `ApplyRoomFilters(rooms, levelFilter, areaFilter)` - Simple filtering
- `ApplyWallFilters(walls, levelFilter, typeFilter)` - Wall filtering
- `ApplyFloorFilters(floors, levelFilter, typeFilter)` - Floor filtering
- `RunAnalysis(roomItems, wallItems, paramMappings, windowHandle)` - Runs room-wall analysis
- `RunFloorAnalysis(roomItems, floorItems, paramMappings, windowHandle)` - Runs room-floor analysis
- `GetAvailableRoomParameters()` - Gets parameters for advanced filtering
- `ApplyAdvancedFilter(RoomFilterConfiguration)` - Applies complex filter

**Usage**:
```csharp
var controller = new RoomWallAnalysisController(
    document,
    elementCollector,
    collisionService,
    loggingService,
    roomFilterService);

// Load data for UI
var initialData = controller.LoadInitialData();

// Apply simple filters
var filteredRooms = controller.ApplyRoomFilters(
    initialData.Rooms,
    "Level 1",
    "100");

// Run analysis
var results = controller.RunAnalysis(
    filteredRooms,
    initialData.Walls,
    parameterMappings,
    windowHandle);
```

---

## Service Dependency Graph

```
RoomDataSyncCommand
    └─> RoomWallAnalysisController
         ├─> IElementCollectorService
         ├─> ICollisionAnalysisService
         │    ├─> IWallBoundaryAnalysisService
         │    │    └─> IParameterMappingExecutionService
         │    └─> IFloorBoundaryAnalysisService
         │         ├─> IParameterMappingExecutionService
         │         ├─> GeometryService
         │         └─> IRoomProcessingService
         ├─> ILoggingService
         └─> RoomFilterService
              ├─> Document
              ├─> RoomParameterDiscoveryService
              ├─> ILoggingService
              └─> IElementCollectorService
```

---

## Common Usage Patterns

### Pattern 1: Full Analysis Workflow

```csharp
// 1. Resolve services
var elementCollector = container.Resolve<IElementCollectorService>();
var collisionService = container.Resolve<ICollisionAnalysisService>();
var loggingService = container.Resolve<ILoggingService>();

// 2. Initialize logging
var logPath = loggingService.InitializeDebugLogging(windowHandle);

// 3. Collect elements
var rooms = elementCollector.GetRooms(document);
var walls = elementCollector.GetWalls(document);

// 4. Configure parameter mappings
var mappings = new List<ParameterMappingConfiguration>
{
    new ParameterMappingConfiguration
    {
        SourceParameterName = "Room Name",
        TargetParameterName = "Comments",
        Direction = MappingDirection.RoomToElement
    }
};

// 5. Create progress reporter
var progressReporter = new ProgressReporter(progressInfo =>
{
    loggingService.WriteToLog($"{progressInfo.Stage}: {progressInfo.Detail}");
    progressWindow?.UpdateProgress(progressInfo);
});

// 6. Run analysis
var results = collisionService.AnalyzeRoomCollisions(
    document,
    rooms,
    walls,
    mappings,
    loggingService.WriteToLog,
    progressReporter);

// 7. Process results
foreach (var result in results)
{
    loggingService.LogInfo($"Room {result.RoomNumber}: {result.WallsColliding} walls");
}
```

### Pattern 2: Advanced Filtering

```csharp
var roomFilter = new RoomFilterService(document, loggingService);

// Create complex filter: (Area > 100 AND Level = "Level 1") OR (Volume > 1000)
var filterConfig = roomFilter.CreateFilterConfiguration("Complex Filter");

// Create first group: Area > 100 AND Level = "Level 1"
var group1 = roomFilter.CreateFilterSet(LogicalOperator.And);
group1.Items.Add(roomFilter.CreateFilterRule("Area", FilterOperator.GreaterThan, "100"));
group1.Items.Add(roomFilter.CreateFilterRule("Level", FilterOperator.Equals, "Level 1"));

// Create second rule: Volume > 1000
var volumeRule = roomFilter.CreateFilterRule("Volume", FilterOperator.GreaterThan, "1000");

// Combine with OR
filterConfig.RootFilterSet.Operator = LogicalOperator.Or;
filterConfig.RootFilterSet.Items.Add(group1);
filterConfig.RootFilterSet.Items.Add(volumeRule);

// Validate
if (!roomFilter.ValidateFilterConfiguration(filterConfig))
{
    loggingService.LogError("Invalid filter configuration");
    return;
}

// Apply
var matchingRooms = roomFilter.ApplyFilter(filterConfig);
loggingService.LogInfo($"Filter matched {matchingRooms.Count} rooms");
```

### Pattern 3: Error Handling

```csharp
try
{
    var results = collisionService.AnalyzeRoomCollisions(...);
}
catch (RevitApiException ex)
{
    loggingService.LogError($"Revit API error: {ex.Message}");
    loggingService.LogError($"Technical details: {ex.TechnicalDetails}");
    TaskDialog.Show("Error", ex.UserMessage);
}
catch (CollisionAnalysisException ex)
{
    loggingService.LogError($"Analysis error: {ex.Message}");
    TaskDialog.Show("Analysis Error", ex.UserMessage);
}
catch (Exception ex)
{
    loggingService.LogError($"Unexpected error: {ex}");
    TaskDialog.Show("Error", "An unexpected error occurred. See log for details.");
}
```

---

**Last Updated**: 2025-10-19
**Version**: 2.0 (Post-refactoring)
