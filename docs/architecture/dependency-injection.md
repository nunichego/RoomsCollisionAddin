# Dependency Injection Guide

## Overview

RoomsManagerAddin uses a lightweight custom dependency injection (DI) container to manage service lifetimes and dependencies. This guide explains how to use DI in the application.

## Why Dependency Injection?

### Benefits

1. **Testability**: Services can be mocked/replaced for testing
2. **Maintainability**: Dependencies are explicit and centralized
3. **Flexibility**: Easy to swap implementations
4. **Separation of Concerns**: Services don't create their dependencies

### Why Custom Container?

- **Lightweight**: ~150 lines of code
- **No External Dependencies**: Revit add-ins should minimize dependencies
- **Simple Requirements**: No need for property injection, scopes, interceptors
- **Full Control**: Easy to understand and debug

## Service Container

### Location

`src/Core/DependencyInjection/ServiceContainer.cs`

### Features

- **Constructor Injection**: Automatically resolves constructor parameters
- **Service Lifetimes**: Singleton and Transient
- **Factory Functions**: Custom object creation
- **Interface/Implementation Mapping**: Register interface, resolve implementation

## Service Lifetimes

### Singleton

**Created once**, shared across all requests.

**Use for**:
- Logging services
- Configuration services
- Stateless services that can be shared

**Example**:
```csharp
services.AddSingleton<ILoggingService, LoggingService>();
```

**Behavior**:
- First `Resolve<ILoggingService>()` creates instance
- Subsequent calls return same instance
- Lives for entire application lifetime

### Transient

**Created every time** it's requested.

**Use for**:
- Document-dependent services
- Stateful services
- Services that shouldn't be shared

**Example**:
```csharp
services.AddTransient<ICollisionAnalysisService, CollisionAnalysisService>();
```

**Behavior**:
- Each `Resolve<ICollisionAnalysisService>()` creates new instance
- Instances are independent
- Short lifetime (garbage collected after use)

## Registration Patterns

### 1. Interface → Implementation

Most common pattern for services with interfaces.

```csharp
services.AddSingleton<ILoggingService, LoggingService>();
services.AddTransient<ICollisionAnalysisService, CollisionAnalysisService>();
```

**Usage**:
```csharp
var loggingService = serviceContainer.Resolve<ILoggingService>();
// Returns LoggingService instance
```

### 2. Concrete Class

For services without interfaces (temporary or utility classes).

```csharp
services.AddTransient<GeometryService>(c => new GeometryService());
```

**Usage**:
```csharp
var geometryService = serviceContainer.Resolve<GeometryService>();
```

### 3. Factory Function

For services requiring custom initialization.

```csharp
services.AddTransient<ProgressReporter>(c =>
{
    var loggingService = c.Resolve<ILoggingService>();
    return new ProgressReporter(loggingService.WriteToLog);
});
```

**Use when**:
- Service requires runtime parameters
- Complex initialization logic
- Conditional creation based on environment

### 4. Self-Registration

Service registered as both interface and concrete class.

```csharp
services.AddSingleton<ILoggingService, LoggingService>();
// Also accessible as LoggingService if needed
```

## Full Registration Example (App.cs)

```csharp
private void ConfigureServices(IServiceContainer services)
{
    // ============================================
    // Infrastructure Services (Singleton)
    // ============================================
    services.AddSingleton<RoomsManagerAddin.Infrastructure.Logging.ILoggingService,
                          RoomsManagerAddin.Infrastructure.Logging.LoggingService>();

    services.AddSingleton<RoomsManagerAddin.Infrastructure.Configuration.IConfigurationService,
                          RoomsManagerAddin.Infrastructure.Configuration.ConfigurationService>();

    // ============================================
    // Revit API Services (Transient - Document-dependent)
    // ============================================
    services.AddTransient<RoomsManagerAddin.Infrastructure.RevitApi.IElementCollectorService,
                          RoomsManagerAddin.Infrastructure.RevitApi.ElementCollectorService>();

    services.AddTransient<RoomsManagerAddin.Infrastructure.RevitApi.GeometryService>(
        c => new RoomsManagerAddin.Infrastructure.RevitApi.GeometryService());

    // ============================================
    // Domain Services - Analysis (Transient)
    // ============================================
    services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.ICollisionAnalysisService,
                          RoomsManagerAddin.Domain.Services.Analysis.CollisionAnalysisService>();

    services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.IWallBoundaryAnalysisService,
                          RoomsManagerAddin.Domain.Services.Analysis.WallBoundaryAnalysisService>();

    services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.IFloorBoundaryAnalysisService,
                          RoomsManagerAddin.Domain.Services.Analysis.FloorBoundaryAnalysisService>();

    // ============================================
    // Domain Services - Filtering (Transient)
    // ============================================
    services.AddTransient<RoomsManagerAddin.Domain.Services.Filtering.IRoomFilterService,
                          RoomsManagerAddin.Domain.Services.Filtering.RoomFilterService>();

    // ... more services
}
```

## Service Resolution

### In Commands

Commands are entry points from Revit. They should resolve services from the global container.

```csharp
public class RoomDataSyncCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Get global service container
        var serviceContainer = App.ServiceContainer;

        // Resolve services
        var elementCollector = serviceContainer.Resolve<IElementCollectorService>();
        var collisionService = serviceContainer.Resolve<ICollisionAnalysisService>();
        var loggingService = serviceContainer.Resolve<ILoggingService>();

        // Create controllers with resolved services
        var controller = new RoomWallAnalysisController(
            document,
            elementCollector,
            collisionService,
            loggingService,
            roomFilterService);

        // Use controller...
    }
}
```

### In Services (Constructor Injection)

Services should receive dependencies through constructor.

```csharp
public class CollisionAnalysisService : ICollisionAnalysisService
{
    private readonly IWallBoundaryAnalysisService _wallBoundaryService;
    private readonly IFloorBoundaryAnalysisService _floorBoundaryService;

    // Dependencies injected via constructor
    public CollisionAnalysisService(
        IWallBoundaryAnalysisService wallBoundaryService,
        IFloorBoundaryAnalysisService floorBoundaryService)
    {
        _wallBoundaryService = wallBoundaryService;
        _floorBoundaryService = floorBoundaryService;
    }

    public List<RoomCollisionResult> AnalyzeRoomCollisions(...)
    {
        // Use injected services
        return _wallBoundaryService.AnalyzeRoomCollisions(...);
    }
}
```

### Automatic Dependency Resolution

The container automatically resolves constructor parameters:

```csharp
// When you resolve CollisionAnalysisService...
var service = container.Resolve<ICollisionAnalysisService>();

// Container automatically:
// 1. Resolves IWallBoundaryAnalysisService → WallBoundaryAnalysisService
// 2. Resolves IFloorBoundaryAnalysisService → FloorBoundaryAnalysisService
// 3. Calls: new CollisionAnalysisService(wallService, floorService)
// 4. Returns the instance
```

## Common Patterns

### Pattern 1: Service with Simple Dependencies

```csharp
// Registration
services.AddTransient<IMyService, MyService>();

// Implementation
public class MyService : IMyService
{
    private readonly ILoggingService _logging;
    private readonly IConfigurationService _config;

    public MyService(ILoggingService logging, IConfigurationService config)
    {
        _logging = logging;
        _config = config;
    }
}

// Usage
var myService = container.Resolve<IMyService>();
```

### Pattern 2: Service Requiring Document

Some services need the Revit `Document`, which isn't in the container.

**Solution**: Pass Document to constructor manually.

```csharp
// Registration
services.AddTransient<IElementCollectorService, ElementCollectorService>();

// Usage in Command
var document = commandData.Application.ActiveUIDocument.Document;
var collector = new ElementCollectorService(document);

// Or if service uses DI:
var loggingService = container.Resolve<ILoggingService>();
var roomFilter = new RoomFilterService(document, loggingService);
```

### Pattern 3: Conditional Service Creation

```csharp
services.AddTransient<IMyService>(container =>
{
    var config = container.Resolve<IConfigurationService>();

    if (config.UseOptimizedVersion)
    {
        return new OptimizedMyService();
    }
    else
    {
        return new StandardMyService();
    }
});
```

### Pattern 4: Service with Callback

```csharp
services.AddTransient<IParameterMappingService>(container =>
{
    var logging = container.Resolve<ILoggingService>();
    return new ParameterMappingService(logging.WriteToLog);
});
```

## Best Practices

### 1. Register Interfaces, Not Concrete Classes

**Good**:
```csharp
services.AddTransient<IMyService, MyService>();
var service = container.Resolve<IMyService>();
```

**Avoid** (unless necessary):
```csharp
services.AddTransient<MyService>();
var service = container.Resolve<MyService>();
```

**Why**: Interfaces enable mocking, flexibility, and decoupling.

### 2. Use Transient for Document-Dependent Services

Revit documents can change, so document-dependent services should be transient.

```csharp
services.AddTransient<IElementCollectorService, ElementCollectorService>();
```

### 3. Use Singleton for Stateless Infrastructure

Logging, configuration, etc. can be shared.

```csharp
services.AddSingleton<ILoggingService, LoggingService>();
```

### 4. Validate Dependencies in Constructors

Always validate injected dependencies:

```csharp
public MyService(ILoggingService logging, IConfigurationService config)
{
    _logging = logging ?? throw new ArgumentNullException(nameof(logging));
    _config = config ?? throw new ArgumentNullException(nameof(config));
}
```

### 5. Don't Store ServiceContainer in Services

**Bad**:
```csharp
public class MyService
{
    private readonly IServiceContainer _container;

    public MyService(IServiceContainer container)
    {
        _container = container; // Anti-pattern!
    }
}
```

**Why**: Services should declare explicit dependencies, not use service locator pattern.

**Good**:
```csharp
public class MyService
{
    private readonly ILoggingService _logging;
    private readonly IConfigurationService _config;

    public MyService(ILoggingService logging, IConfigurationService config)
    {
        _logging = logging;
        _config = config;
    }
}
```

## Troubleshooting

### Error: "No service registered for type X"

**Cause**: Service not registered in `App.ConfigureServices()`.

**Solution**: Add registration:
```csharp
services.AddTransient<IMyService, MyService>();
```

### Error: "Circular dependency detected"

**Cause**: Service A depends on Service B, and Service B depends on Service A.

**Solution**: Refactor to break circular dependency (extract common logic to new service).

### Error: "Cannot resolve type X with constructor parameters"

**Cause**: Constructor parameters not registered in container.

**Solution**: Ensure all constructor parameter types are registered:
```csharp
// If MyService(ILoggingService logging) fails:
services.AddSingleton<ILoggingService, LoggingService>();
```

## Adding a New Service

### Step 1: Create Interface

```csharp
// src/Domain/Services/MyService/IMyService.cs
namespace RoomsManagerAddin.Domain.Services.MyService
{
    public interface IMyService
    {
        void DoSomething();
    }
}
```

### Step 2: Implement Service

```csharp
// src/Domain/Services/MyService/MyService.cs
namespace RoomsManagerAddin.Domain.Services.MyService
{
    public class MyService : IMyService
    {
        private readonly ILoggingService _logging;

        public MyService(ILoggingService logging)
        {
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        public void DoSomething()
        {
            _logging.LogInfo("Doing something");
            // Implementation...
        }
    }
}
```

### Step 3: Register in DI Container

```csharp
// App.cs - ConfigureServices method
services.AddTransient<RoomsManagerAddin.Domain.Services.MyService.IMyService,
                      RoomsManagerAddin.Domain.Services.MyService.MyService>();
```

### Step 4: Use Service

```csharp
// In a command or another service
var myService = serviceContainer.Resolve<IMyService>();
myService.DoSomething();
```

## Advanced: Factory Pattern with DI

Sometimes you need to create multiple instances with different configurations:

```csharp
// Interface
public interface IMyServiceFactory
{
    IMyService Create(string config);
}

// Factory Implementation
public class MyServiceFactory : IMyServiceFactory
{
    private readonly ILoggingService _logging;

    public MyServiceFactory(ILoggingService logging)
    {
        _logging = logging;
    }

    public IMyService Create(string config)
    {
        return new MyService(_logging, config);
    }
}

// Registration
services.AddSingleton<IMyServiceFactory, MyServiceFactory>();

// Usage
var factory = container.Resolve<IMyServiceFactory>();
var service1 = factory.Create("config1");
var service2 = factory.Create("config2");
```

## Migration from Manual Instantiation

### Before (Manual)

```csharp
public Result Execute(...)
{
    var logging = new LoggingService();
    var geometryService = new GeometryService();
    var paramMapping = new ParameterMappingExecutionService(logging.WriteToLog);
    var wallBoundary = new WallBoundaryAnalysisService(paramMapping);
    var floorBoundary = new FloorBoundaryAnalysisService(paramMapping, geometryService, ...);
    var collisionService = new CollisionAnalysisService(wallBoundary, floorBoundary);

    // Use services...
}
```

### After (DI)

```csharp
public Result Execute(...)
{
    var serviceContainer = App.ServiceContainer;
    var logging = serviceContainer.Resolve<ILoggingService>();
    var collisionService = serviceContainer.Resolve<ICollisionAnalysisService>();

    // Container handles all dependency resolution automatically!
}
```

---

**Last Updated**: 2025-10-19
**Version**: 2.0 (Post-refactoring)
