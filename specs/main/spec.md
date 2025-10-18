# Refactoring Specification: RoomsManagerAddin Best Practices Implementation

**Project**: RoomsManagerAddin for Revit 2024
**Date**: 2025-10-18
**Type**: Comprehensive Refactoring (Structure + Code Quality)
**Compatibility**: Full backward compatibility required

---

## Executive Summary

This specification outlines a comprehensive refactoring of the RoomsManagerAddin to align with industry best practices while maintaining 100% backward compatibility with existing functionality. The refactoring focuses on:

1. **Project Structure**: Reorganizing folders and namespaces for better maintainability
2. **Dependency Injection**: Implementing proper DI patterns for testability
3. **Error Handling**: Standardizing exception handling and recovery
4. **Documentation**: Adding comprehensive XML documentation

---

## Table of Contents

1. [Current State Analysis](#1-current-state-analysis)
2. [Target Architecture](#2-target-architecture)
3. [Folder Structure Reorganization](#3-folder-structure-reorganization)
4. [Dependency Injection Implementation](#4-dependency-injection-implementation)
5. [Error Handling Improvements](#5-error-handling-improvements)
6. [Documentation Standards](#6-documentation-standards)
7. [Implementation Phases](#7-implementation-phases)
8. [Testing Strategy](#8-testing-strategy)
9. [Risk Mitigation](#9-risk-mitigation)
10. [Success Criteria](#10-success-criteria)

---

## 1. Current State Analysis

### 1.1 Current Folder Structure

```
RoomsCollisionAddin/
├── Commands/                     (4 files)
├── Controllers/                  (2 files)
├── DevelopmentHistory/
├── Examples/
├── Models/                       (4 files)
├── Properties/
├── RoomsManager/
├── Services/                     (18 files - flat structure)
│   ├── categories/
│   │   ├── Walls/
│   │   └── Floors/
├── UI/
├── Windows/
├── Resources/
│   ├── documents/
│   ├── icons/
│   └── templates/
├── App.cs                        (root level)
├── RoomWallAnalysisWindow.xaml.cs (root level)
└── ProgressWindow.cs             (root level)
```

### 1.2 Identified Issues

#### Project Organization Issues
- ✗ UI files mixed in root directory (Window files should be in `Windows/` or `Views/`)
- ✗ Services folder is flat with 18 files (lacks subcategorization)
- ✗ Inconsistent namespace usage across services
- ✗ No clear separation between domain services and infrastructure services
- ✗ Template files and development history mixed with source code

#### Dependency Management Issues
- ✗ Manual service instantiation in constructors (tight coupling)
- ✗ No dependency injection container
- ✗ Services create their own dependencies instead of receiving them
- ✗ Difficult to mock dependencies for unit testing
- ✗ No service lifetime management

#### Error Handling Issues
- ✗ Inconsistent exception handling patterns
- ✗ Some methods swallow exceptions silently
- ✗ Generic catch blocks without proper logging
- ✗ No standardized error recovery strategy
- ✗ UI error messages not user-friendly

#### Documentation Issues
- ✗ Many public methods lack XML documentation
- ✗ Complex algorithms not explained with comments
- ✗ No usage examples for complex services
- ✗ Missing interface documentation

---

## 2. Target Architecture

### 2.1 Architectural Principles

1. **Layered Architecture**
   - Presentation Layer (UI/Windows)
   - Application Layer (Controllers/Commands)
   - Domain Layer (Services/Models)
   - Infrastructure Layer (Revit API Access)

2. **Dependency Injection**
   - Constructor injection for all dependencies
   - Service container for lifetime management
   - Interface-based abstractions

3. **SOLID Principles**
   - Single Responsibility: One service, one purpose
   - Open/Closed: Extensible without modification
   - Liskov Substitution: Interface-based design
   - Interface Segregation: Focused interfaces
   - Dependency Inversion: Depend on abstractions

### 2.2 Technology Stack

- **.NET Framework 4.8** (required for Revit 2024)
- **WPF/XAML** for UI
- **Revit API 2024**
- **Simple DI Container** (lightweight, no external dependencies required)

---

## 3. Folder Structure Reorganization

### 3.1 Target Folder Structure

```
RoomsCollisionAddin/
│
├── src/                          [NEW: All source code]
│   │
│   ├── Application/              [NEW: Application layer]
│   │   ├── Commands/
│   │   │   ├── BaseCommand.cs
│   │   │   ├── RoomDataSyncCommand.cs
│   │   │   ├── SettingsCommand.cs
│   │   │   └── HelpCommand.cs
│   │   │
│   │   └── Controllers/
│   │       ├── RoomWallAnalysisController.cs
│   │       └── GenericElementController.cs
│   │
│   ├── Domain/                   [NEW: Domain layer]
│   │   ├── Services/
│   │   │   ├── Analysis/         [NEW: Analysis services]
│   │   │   │   ├── CollisionAnalysisService.cs
│   │   │   │   ├── WallBoundaryAnalysisService.cs
│   │   │   │   └── FloorBoundaryAnalysisService.cs
│   │   │   │
│   │   │   ├── Filtering/        [NEW: Filter services]
│   │   │   │   ├── RoomFilterService.cs
│   │   │   │   ├── GenericElementFilterService.cs
│   │   │   │   ├── RoomParameterDiscoveryService.cs
│   │   │   │   └── ElementParameterDiscoveryService.cs
│   │   │   │
│   │   │   ├── Processing/       [NEW: Processing services]
│   │   │   │   ├── RoomProcessingService.cs
│   │   │   │   ├── WallProcessingService.cs
│   │   │   │   └── ParameterMappingExecutionService.cs
│   │   │   │
│   │   │   └── Mapping/          [NEW: Parameter mapping]
│   │   │       └── ParameterMappingService.cs
│   │   │
│   │   └── Models/
│   │       ├── Analysis/          [NEW: Analysis models]
│   │       │   ├── RoomCollisionResult.cs
│   │       │   └── CollisionAnalysisOptions.cs
│   │       │
│   │       ├── Filtering/         [NEW: Filter models]
│   │       │   ├── FilterModels.cs
│   │       │   ├── RoomFilterConfiguration.cs
│   │       │   └── FilterRule.cs
│   │       │
│   │       ├── Shared/            [NEW: Shared models]
│   │       │   ├── SharedModels.cs
│   │       │   ├── RoomItem.cs
│   │       │   ├── WallItem.cs
│   │       │   └── FloorItem.cs
│   │       │
│   │       └── Configuration/     [NEW: Config models]
│   │           └── AppSettings.cs
│   │
│   ├── Infrastructure/           [NEW: Infrastructure layer]
│   │   ├── RevitApi/             [NEW: Revit API wrappers]
│   │   │   ├── ElementCollectorService.cs
│   │   │   ├── GeometryService.cs
│   │   │   └── ParameterUpdateService.cs
│   │   │
│   │   ├── Logging/              [NEW: Logging infrastructure]
│   │   │   ├── ILoggingService.cs
│   │   │   ├── LoggingService.cs
│   │   │   ├── FileLogger.cs
│   │   │   └── LogLevel.cs
│   │   │
│   │   ├── Configuration/        [NEW: Configuration services]
│   │   │   ├── IConfigurationService.cs
│   │   │   └── ConfigurationService.cs
│   │   │
│   │   └── Progress/             [NEW: Progress reporting]
│   │       ├── IProgressReporter.cs
│   │       ├── ProgressReporter.cs
│   │       ├── ProgressService.cs
│   │       └── ProgressInfo.cs
│   │
│   ├── Presentation/             [NEW: Presentation layer]
│   │   ├── Views/                [RENAMED from Windows]
│   │   │   ├── RoomWallAnalysisWindow.xaml
│   │   │   ├── RoomWallAnalysisWindow.xaml.cs
│   │   │   ├── ModernProgressWindow.xaml
│   │   │   └── ModernProgressWindow.xaml.cs
│   │   │
│   │   └── Controls/             [NEW: Custom controls]
│   │       └── FilterRulesPanel.cs
│   │
│   ├── Core/                     [NEW: Core components]
│   │   ├── DependencyInjection/
│   │   │   ├── IServiceContainer.cs
│   │   │   ├── ServiceContainer.cs
│   │   │   ├── ServiceDescriptor.cs
│   │   │   └── ServiceLifetime.cs
│   │   │
│   │   ├── Exceptions/           [NEW: Custom exceptions]
│   │   │   ├── RoomsManagerException.cs
│   │   │   ├── RevitApiException.cs
│   │   │   ├── FilterValidationException.cs
│   │   │   └── CollisionAnalysisException.cs
│   │   │
│   │   └── Extensions/           [NEW: Extension methods]
│   │       ├── RevitExtensions.cs
│   │       ├── GeometryExtensions.cs
│   │       └── CollectionExtensions.cs
│   │
│   └── App.cs                    [Root application class]
│
├── Resources/
│   ├── documents/                [Documentation only]
│   ├── icons/                    [Embedded resources]
│   └── templates/                [Code templates, not built]
│
├── Tests/                        [NEW: Unit tests]
│   ├── Unit/
│   │   ├── Services/
│   │   ├── Controllers/
│   │   └── Models/
│   │
│   └── Integration/
│       └── RevitApi/
│
├── docs/                         [NEW: Documentation]
│   ├── architecture/
│   ├── api/
│   └── guides/
│
├── Properties/
├── .speckit/                     [SpecKit files]
├── RoomsManagerAddin.addin
├── RoomsManagerAddin.csproj
└── README.md
```

### 3.2 Namespace Reorganization

```csharp
// Root namespace
RoomsManagerAddin

// Application layer
RoomsManagerAddin.Application.Commands
RoomsManagerAddin.Application.Controllers

// Domain layer
RoomsManagerAddin.Domain.Services.Analysis
RoomsManagerAddin.Domain.Services.Filtering
RoomsManagerAddin.Domain.Services.Processing
RoomsManagerAddin.Domain.Services.Mapping
RoomsManagerAddin.Domain.Models.Analysis
RoomsManagerAddin.Domain.Models.Filtering
RoomsManagerAddin.Domain.Models.Shared
RoomsManagerAddin.Domain.Models.Configuration

// Infrastructure layer
RoomsManagerAddin.Infrastructure.RevitApi
RoomsManagerAddin.Infrastructure.Logging
RoomsManagerAddin.Infrastructure.Configuration
RoomsManagerAddin.Infrastructure.Progress

// Presentation layer
RoomsManagerAddin.Presentation.Views
RoomsManagerAddin.Presentation.Controls

// Core
RoomsManagerAddin.Core.DependencyInjection
RoomsManagerAddin.Core.Exceptions
RoomsManagerAddin.Core.Extensions
```

### 3.3 File Migration Map

| Current Location | Target Location | Notes |
|-----------------|-----------------|-------|
| `App.cs` | `src/App.cs` | Root app class stays at src root |
| `Commands/*.cs` | `src/Application/Commands/` | No changes to code |
| `Controllers/*.cs` | `src/Application/Controllers/` | No changes to code |
| `Services/CollisionAnalysisService*.cs` | `src/Domain/Services/Analysis/` | Category-specific organization |
| `Services/*FilterService.cs` | `src/Domain/Services/Filtering/` | Filtering services grouped |
| `Services/*ProcessingService.cs` | `src/Domain/Services/Processing/` | Processing services grouped |
| `Services/ParameterMapping*.cs` | `src/Domain/Services/Mapping/` | Mapping services grouped |
| `Services/ElementCollectorService.cs` | `src/Infrastructure/RevitApi/` | Infrastructure concern |
| `Services/GeometryService.cs` | `src/Infrastructure/RevitApi/` | Infrastructure concern |
| `Services/ParameterUpdateService.cs` | `src/Infrastructure/RevitApi/` | Infrastructure concern |
| `Services/LoggingService.cs` | `src/Infrastructure/Logging/` | Extract interface |
| `Services/ProgressService.cs` | `src/Infrastructure/Progress/` | Extract interface |
| `Services/ProgressReporter.cs` | `src/Infrastructure/Progress/` | Extract interface |
| `Services/ConfigurationService.cs` | `src/Infrastructure/Configuration/` | Already has interface |
| `Models/SharedModels.cs` | Split into `src/Domain/Models/Shared/` | Split into separate files |
| `Models/FilterModels.cs` | Split into `src/Domain/Models/Filtering/` | Split into separate files |
| `Windows/*.xaml` | `src/Presentation/Views/` | XAML + code-behind |
| `RoomWallAnalysisWindow.xaml.cs` | `src/Presentation/Views/` | Move from root |
| `UI/FilterRulesPanel.cs` | `src/Presentation/Controls/` | Custom control |

---

## 4. Dependency Injection Implementation

### 4.1 Service Container Design

Create a lightweight DI container without external dependencies:

```csharp
// src/Core/DependencyInjection/ServiceLifetime.cs
namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service in the container
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>New instance created each time</summary>
        Transient,

        /// <summary>Single instance per scope (not implemented for simplicity)</summary>
        Scoped,

        /// <summary>Single instance for application lifetime</summary>
        Singleton
    }
}

// src/Core/DependencyInjection/ServiceDescriptor.cs
namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Describes a service registration
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public object ImplementationInstance { get; set; }
        public Func<IServiceContainer, object> ImplementationFactory { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }
}

// src/Core/DependencyInjection/IServiceContainer.cs
namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Service container for dependency injection
    /// </summary>
    public interface IServiceContainer
    {
        /// <summary>Register a service with transient lifetime</summary>
        void AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>Register a service with singleton lifetime</summary>
        void AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>Register a singleton instance</summary>
        void AddSingleton<TService>(TService instance) where TService : class;

        /// <summary>Register a service with a factory</summary>
        void AddTransient<TService>(Func<IServiceContainer, TService> factory)
            where TService : class;

        /// <summary>Resolve a service</summary>
        TService Resolve<TService>() where TService : class;

        /// <summary>Resolve a service by type</summary>
        object Resolve(Type serviceType);

        /// <summary>Check if service is registered</summary>
        bool IsRegistered<TService>() where TService : class;
    }
}

// src/Core/DependencyInjection/ServiceContainer.cs
namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Simple dependency injection container implementation
    /// </summary>
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services;
        private readonly Dictionary<Type, object> _singletonInstances;

        public ServiceContainer()
        {
            _services = new Dictionary<Type, ServiceDescriptor>();
            _singletonInstances = new Dictionary<Type, object>();
        }

        public void AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Transient
            };
        }

        public void AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Singleton
            };
        }

        public void AddSingleton<TService>(TService instance) where TService : class
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationInstance = instance,
                Lifetime = ServiceLifetime.Singleton
            };
            _singletonInstances[typeof(TService)] = instance;
        }

        public void AddTransient<TService>(Func<IServiceContainer, TService> factory)
            where TService : class
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationFactory = container => factory(container),
                Lifetime = ServiceLifetime.Transient
            };
        }

        public TService Resolve<TService>() where TService : class
        {
            return (TService)Resolve(typeof(TService));
        }

        public object Resolve(Type serviceType)
        {
            if (!_services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException(
                    $"Service of type {serviceType.Name} is not registered");
            }

            var descriptor = _services[serviceType];

            // Return singleton instance if already created
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                if (_singletonInstances.ContainsKey(serviceType))
                {
                    return _singletonInstances[serviceType];
                }
            }

            // Create instance
            object instance;

            if (descriptor.ImplementationInstance != null)
            {
                instance = descriptor.ImplementationInstance;
            }
            else if (descriptor.ImplementationFactory != null)
            {
                instance = descriptor.ImplementationFactory(this);
            }
            else
            {
                instance = CreateInstance(descriptor.ImplementationType);
            }

            // Cache singleton
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _singletonInstances[serviceType] = instance;
            }

            return instance;
        }

        private object CreateInstance(Type type)
        {
            // Get constructor with most parameters (assumes DI constructor)
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No public constructors found for {type.Name}");
            }

            var constructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterInstances[i] = Resolve(parameters[i].ParameterType);
            }

            return constructor.Invoke(parameterInstances);
        }

        public bool IsRegistered<TService>() where TService : class
        {
            return _services.ContainsKey(typeof(TService));
        }
    }
}
```

### 4.2 Service Registration

Update `App.cs` to register all services:

```csharp
// src/App.cs
using RoomsManagerAddin.Core.DependencyInjection;
using RoomsManagerAddin.Infrastructure.Logging;
using RoomsManagerAddin.Infrastructure.Configuration;
// ... other usings

namespace RoomsManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        private static IServiceContainer _serviceContainer;

        /// <summary>
        /// Global service container (accessible to commands)
        /// </summary>
        public static IServiceContainer ServiceContainer => _serviceContainer;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Initialize DI container
                _serviceContainer = new ServiceContainer();
                ConfigureServices(_serviceContainer);

                // Get assembly path
                _assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Create ribbon panel
                CreateRibbonPanel(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to start: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Configure dependency injection services
        /// </summary>
        private void ConfigureServices(IServiceContainer services)
        {
            // Infrastructure services (Singleton)
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Revit API services (Transient - Document-dependent)
            services.AddTransient<IElementCollectorService, ElementCollectorService>();
            services.AddTransient<IGeometryService, GeometryService>();
            services.AddTransient<IParameterUpdateService, ParameterUpdateService>();

            // Analysis services (Transient)
            services.AddTransient<ICollisionAnalysisService, CollisionAnalysisService>();
            services.AddTransient<IWallBoundaryAnalysisService, WallBoundaryAnalysisService>();
            services.AddTransient<IFloorBoundaryAnalysisService, FloorBoundaryAnalysisService>();

            // Filtering services (Transient)
            services.AddTransient<IRoomFilterService, RoomFilterService>();
            services.AddTransient<IGenericElementFilterService, GenericElementFilterService>();
            services.AddTransient<IRoomParameterDiscoveryService, RoomParameterDiscoveryService>();
            services.AddTransient<IElementParameterDiscoveryService, ElementParameterDiscoveryService>();

            // Processing services (Transient)
            services.AddTransient<IRoomProcessingService, RoomProcessingService>();
            services.AddTransient<IWallProcessingService, WallProcessingService>();
            services.AddTransient<IParameterMappingExecutionService, ParameterMappingExecutionService>();

            // Mapping services (Transient)
            services.AddTransient<IParameterMappingService, ParameterMappingService>();

            // Progress reporting (Transient)
            services.AddTransient<IProgressReporter, ProgressReporter>();
            services.AddTransient<IProgressService, ProgressService>();

            // Controllers (Transient - created per command invocation)
            services.AddTransient<RoomWallAnalysisController>(container =>
            {
                // Note: Document will be passed in constructor when resolved
                throw new InvalidOperationException(
                    "RoomWallAnalysisController requires Document parameter. " +
                    "Create manually in command.");
            });
        }
    }
}
```

### 4.3 Extract Service Interfaces

Create interfaces for all major services:

```csharp
// Example: src/Infrastructure/Logging/ILoggingService.cs
namespace RoomsManagerAddin.Infrastructure.Logging
{
    /// <summary>
    /// Service for application logging
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>Initialize logging with optional file path</summary>
        string InitializeDebugLogging(IntPtr? ownerWindowHandle = null);

        /// <summary>Write message to log</summary>
        void WriteToLog(string message);

        /// <summary>Log informational message</summary>
        void LogInfo(string message);

        /// <summary>Log warning message</summary>
        void LogWarning(string message);

        /// <summary>Log error message</summary>
        void LogError(string message);

        /// <summary>Get current log file path</summary>
        string GetDebugLogPath();
    }
}

// Example: src/Infrastructure/RevitApi/IElementCollectorService.cs
namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for collecting Revit elements
    /// </summary>
    public interface IElementCollectorService
    {
        /// <summary>Get all rooms in document</summary>
        List<Room> GetRooms(Document document);

        /// <summary>Get all walls in document</summary>
        List<Wall> GetWalls(Document document);

        /// <summary>Get all floors in document</summary>
        List<Floor> GetFloors(Document document);

        /// <summary>Get elements by category</summary>
        List<Element> GetElementsByCategory(Document document, BuiltInCategory category);
    }
}

// Example: src/Domain/Services/Analysis/ICollisionAnalysisService.cs
namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Service for analyzing collisions between rooms and elements
    /// </summary>
    public interface ICollisionAnalysisService
    {
        /// <summary>Analyze room-wall collisions</summary>
        List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Wall> walls,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter);

        /// <summary>Analyze room-floor collisions</summary>
        List<RoomCollisionResult> AnalyzeRoomFloorsCollisions(
            Document document,
            List<Room> rooms,
            List<Floor> floors,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter);
    }
}
```

### 4.4 Update Service Implementations

Update existing services to implement interfaces and use constructor injection:

```csharp
// Example: Updated CollisionAnalysisService
namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Service for analyzing collisions between rooms and other elements
    /// </summary>
    public class CollisionAnalysisService : ICollisionAnalysisService
    {
        private readonly IWallBoundaryAnalysisService _wallBoundaryService;
        private readonly IFloorBoundaryAnalysisService _floorBoundaryService;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public CollisionAnalysisService(
            IWallBoundaryAnalysisService wallBoundaryService,
            IFloorBoundaryAnalysisService floorBoundaryService,
            ILoggingService loggingService)
        {
            _wallBoundaryService = wallBoundaryService
                ?? throw new ArgumentNullException(nameof(wallBoundaryService));
            _floorBoundaryService = floorBoundaryService
                ?? throw new ArgumentNullException(nameof(floorBoundaryService));
            _loggingService = loggingService
                ?? throw new ArgumentNullException(nameof(loggingService));
        }

        // ... existing methods
    }
}
```

### 4.5 Update Commands to Use DI

```csharp
// Example: Updated RoomDataSyncCommand
namespace RoomsManagerAddin.Application.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RoomDataSyncCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                var document = commandData.Application.ActiveUIDocument.Document;

                // Resolve controller from DI container
                // Note: Controller requires Document, so create manually
                var loggingService = App.ServiceContainer.Resolve<ILoggingService>();
                var controller = new RoomWallAnalysisController(
                    document,
                    App.ServiceContainer);

                // Open window
                var window = new RoomWallAnalysisWindow(document, controller);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
```

---

## 5. Error Handling Improvements

### 5.1 Custom Exception Hierarchy

```csharp
// src/Core/Exceptions/RoomsManagerException.cs
namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Base exception for all RoomsManager errors
    /// </summary>
    public class RoomsManagerException : Exception
    {
        /// <summary>User-friendly error message</summary>
        public string UserMessage { get; set; }

        /// <summary>Technical details for logging</summary>
        public string TechnicalDetails { get; set; }

        /// <summary>Indicates if error should be shown to user</summary>
        public bool ShowToUser { get; set; } = true;

        public RoomsManagerException(string message) : base(message)
        {
            UserMessage = message;
        }

        public RoomsManagerException(string message, Exception innerException)
            : base(message, innerException)
        {
            UserMessage = message;
            TechnicalDetails = innerException?.ToString();
        }

        public RoomsManagerException(string userMessage, string technicalDetails)
            : base(userMessage)
        {
            UserMessage = userMessage;
            TechnicalDetails = technicalDetails;
        }
    }
}

// src/Core/Exceptions/RevitApiException.cs
namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when Revit API operations fail
    /// </summary>
    public class RevitApiException : RoomsManagerException
    {
        public string RevitOperation { get; set; }

        public RevitApiException(string operation, Exception innerException)
            : base($"Revit API error during {operation}", innerException)
        {
            RevitOperation = operation;
            UserMessage = $"An error occurred while accessing Revit elements. " +
                         $"Please check that the document is valid and try again.";
        }
    }
}

// src/Core/Exceptions/FilterValidationException.cs
namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when filter validation fails
    /// </summary>
    public class FilterValidationException : RoomsManagerException
    {
        public string FilterRule { get; set; }
        public string ValidationError { get; set; }

        public FilterValidationException(string rule, string error)
            : base($"Filter validation failed: {error}")
        {
            FilterRule = rule;
            ValidationError = error;
            UserMessage = $"The filter rule is invalid: {error}. " +
                         $"Please correct the filter and try again.";
        }
    }
}

// src/Core/Exceptions/CollisionAnalysisException.cs
namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Exception thrown during collision analysis
    /// </summary>
    public class CollisionAnalysisException : RoomsManagerException
    {
        public string AnalysisPhase { get; set; }

        public CollisionAnalysisException(string phase, Exception innerException)
            : base($"Collision analysis failed during {phase}", innerException)
        {
            AnalysisPhase = phase;
            UserMessage = $"The collision analysis encountered an error. " +
                         $"Some results may be incomplete. Please check the log for details.";
        }
    }
}
```

### 5.2 Error Handling Patterns

```csharp
// Pattern 1: Service method with proper error handling
public List<Room> GetRooms(Document document)
{
    if (document == null)
        throw new ArgumentNullException(nameof(document));

    try
    {
        var collector = new FilteredElementCollector(document)
            .OfClass(typeof(SpatialElement))
            .OfCategory(BuiltInCategory.OST_Rooms);

        var rooms = collector
            .Cast<Room>()
            .Where(r => r.Area > 0)
            .ToList();

        _loggingService?.LogInfo($"Collected {rooms.Count} rooms from document");

        return rooms;
    }
    catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
    {
        throw new RevitApiException("collecting rooms", ex);
    }
    catch (Exception ex)
    {
        _loggingService?.LogError($"Unexpected error collecting rooms: {ex.Message}");
        throw new RoomsManagerException(
            "Failed to collect rooms from document",
            ex.ToString());
    }
}

// Pattern 2: Controller method with user-facing error handling
public void RunAnalysis()
{
    try
    {
        _loggingService.LogInfo("Starting collision analysis");

        // Validate inputs
        ValidateAnalysisInputs();

        // Run analysis
        var results = _collisionAnalysisService.AnalyzeRoomCollisions(
            _document, _rooms, _walls, _mappings,
            _loggingService.WriteToLog, _progressReporter);

        // Show results
        ShowResults(results);

        _loggingService.LogInfo("Analysis completed successfully");
    }
    catch (RoomsManagerException ex)
    {
        _loggingService.LogError($"Analysis error: {ex.Message}");
        _loggingService.LogError($"Technical details: {ex.TechnicalDetails}");

        if (ex.ShowToUser)
        {
            MessageBox.Show(
                ex.UserMessage,
                "Analysis Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        _loggingService.LogError($"Unexpected error: {ex}");

        MessageBox.Show(
            "An unexpected error occurred during analysis. " +
            "Please check the log file for details and contact support if the problem persists.",
            "Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

// Pattern 3: Validation with specific exceptions
private void ValidateAnalysisInputs()
{
    if (_rooms == null || _rooms.Count == 0)
    {
        throw new RoomsManagerException(
            "No rooms available for analysis. " +
            "Please ensure the document contains placed rooms.")
        {
            ShowToUser = true
        };
    }

    if (_walls == null || _walls.Count == 0)
    {
        throw new RoomsManagerException(
            "No walls available for analysis. " +
            "Please ensure the document contains walls.")
        {
            ShowToUser = true
        };
    }

    if (_filterConfiguration != null)
    {
        try
        {
            _filterService.ValidateFilter(_filterConfiguration);
        }
        catch (Exception ex)
        {
            throw new FilterValidationException(
                _filterConfiguration.Name,
                ex.Message);
        }
    }
}
```

### 5.3 Global Error Handler

```csharp
// src/Core/ErrorHandling/GlobalErrorHandler.cs
namespace RoomsManagerAddin.Core.ErrorHandling
{
    /// <summary>
    /// Centralized error handling and reporting
    /// </summary>
    public static class GlobalErrorHandler
    {
        private static ILoggingService _loggingService;

        /// <summary>Initialize error handler with logging service</summary>
        public static void Initialize(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Handle exception with logging and user notification
        /// </summary>
        public static void HandleException(
            Exception ex,
            string context,
            bool showToUser = true)
        {
            // Log error
            _loggingService?.LogError($"[{context}] {ex.Message}");
            _loggingService?.LogError($"Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                _loggingService?.LogError($"Inner exception: {ex.InnerException}");
            }

            // Show to user if requested
            if (showToUser)
            {
                var userMessage = GetUserFriendlyMessage(ex, context);

                TaskDialog.Show(
                    "Error",
                    userMessage,
                    TaskDialogCommonButtons.Ok);
            }
        }

        /// <summary>
        /// Get user-friendly error message
        /// </summary>
        private static string GetUserFriendlyMessage(Exception ex, string context)
        {
            if (ex is RoomsManagerException rmEx)
            {
                return rmEx.UserMessage;
            }

            if (ex is Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return $"A Revit operation failed during {context}. " +
                       "Please check that the document is valid and try again.";
            }

            if (ex is UnauthorizedAccessException)
            {
                return "Access denied. Please check file permissions and try again.";
            }

            if (ex is ArgumentNullException argEx)
            {
                return $"Required information is missing: {argEx.ParamName}. " +
                       "Please check your inputs and try again.";
            }

            // Generic message
            return $"An error occurred during {context}. " +
                   $"Error: {ex.Message}\n\n" +
                   "Please check the log file for more details.";
        }

        /// <summary>
        /// Wrap an action with error handling
        /// </summary>
        public static Result ExecuteWithErrorHandling(
            string context,
            Action action,
            ref string message)
        {
            try
            {
                action();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HandleException(ex, context, showToUser: true);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
```

---

## 6. Documentation Standards

### 6.1 XML Documentation Template

All public members must have XML documentation:

```csharp
/// <summary>
/// [Brief one-line description]
/// </summary>
/// <remarks>
/// [Optional: Detailed description, usage notes, algorithm explanation]
///
/// Example usage:
/// <code>
/// var service = new CollisionAnalysisService(wallService, floorService, logger);
/// var results = service.AnalyzeRoomCollisions(doc, rooms, walls, mappings, log, progress);
/// </code>
/// </remarks>
/// <param name="paramName">[Description of parameter]</param>
/// <returns>[Description of return value]</returns>
/// <exception cref="ExceptionType">[When this exception is thrown]</exception>
```

### 6.2 Documentation Requirements by Component Type

#### Services
```csharp
/// <summary>
/// Service for [primary responsibility]
/// </summary>
/// <remarks>
/// This service handles [detailed purpose]. It is designed to be [stateless/stateful]
/// and should be registered as [Transient/Singleton] in the DI container.
///
/// Dependencies:
/// - [Dependency1]: [Why needed]
/// - [Dependency2]: [Why needed]
///
/// Thread Safety: [Thread-safe/Not thread-safe]
/// Performance: [Any performance considerations]
/// </remarks>
public class SomeService : ISomeService
{
    /// <summary>
    /// [Method purpose]
    /// </summary>
    /// <remarks>
    /// [Algorithm description if complex]
    /// [Performance characteristics]
    /// [Side effects if any]
    /// </remarks>
    /// <param name="param1">[Purpose and constraints]</param>
    /// <returns>[What is returned and when]</returns>
    /// <exception cref="ArgumentNullException">If param1 is null</exception>
    /// <exception cref="RevitApiException">If Revit API operation fails</exception>
    public ReturnType MethodName(Type param1)
    {
        // Implementation
    }
}
```

#### Models
```csharp
/// <summary>
/// Represents [what this model represents]
/// </summary>
/// <remarks>
/// This model is used to [purpose]. It is typically [created/populated] by [source]
/// and consumed by [consumers].
/// </remarks>
public class SomeModel
{
    /// <summary>
    /// [Property purpose and meaning]
    /// </summary>
    /// <remarks>
    /// [Value range, units, constraints]
    /// </remarks>
    public string PropertyName { get; set; }
}
```

#### Controllers
```csharp
/// <summary>
/// Orchestrates [workflow name] workflow
/// </summary>
/// <remarks>
/// This controller coordinates between [services] to accomplish [goal].
/// It manages [state/UI/data] for [window/feature].
///
/// Lifecycle: [When created and destroyed]
/// </remarks>
public class SomeController
{
    // Documentation similar to services
}
```

### 6.3 Code Comment Guidelines

```csharp
public void ComplexMethod()
{
    // HIGH-LEVEL: What this section does (why, not what)
    // Good: "Pre-filter walls by Z-axis to reduce expensive solid intersections"
    // Bad: "Loop through walls"

    // ALGORITHM: Explain complex logic
    // Good: "Use binary search since walls are sorted by elevation"
    // Bad: "Search for wall"

    // WORKAROUND: Explain Revit API quirks
    // Good: "Room.get_Geometry() fails for unplaced rooms, use SEGC as fallback"
    // Bad: "Try different method"

    // PERFORMANCE: Explain optimization trade-offs
    // Good: "Pre-compute bounding boxes (50ms) to save 90% on solid intersection (2000ms)"
    // Bad: "Cache boxes"

    // TODO: What needs to be done (with date and name)
    // TODO(2025-10-18, John): Implement cancellation support

    // HACK: Temporary workaround that should be fixed
    // HACK: Using string comparison until parameter ID lookup is implemented

    // NOTE: Important information
    // NOTE: This assumes walls are sorted by elevation from previous step
}
```

### 6.4 README and Documentation Files

Create comprehensive documentation:

```markdown
# docs/architecture/overview.md
- High-level architecture diagram
- Layer responsibilities
- Data flow diagrams
- Key design decisions

# docs/architecture/dependency-injection.md
- DI container usage
- Service registration
- Service lifetimes
- Resolving dependencies

# docs/api/services.md
- Service catalog
- Service responsibilities
- Dependencies
- Usage examples

# docs/guides/adding-features.md
- Step-by-step guide for adding new features
- Where to add code
- How to register services
- Testing strategy

# docs/guides/debugging.md
- Debugging tips
- Logging configuration
- Common issues and solutions
```

---

## 7. Implementation Phases

### Phase 1: Foundation (Week 1)
**Goal**: Set up new structure without breaking existing functionality

**Tasks**:
1. Create new folder structure (`src/`, subfolders)
2. Implement DI container (`ServiceContainer`, interfaces)
3. Create custom exception classes
4. Create service interfaces (start with infrastructure services)
5. Update `App.cs` with service registration

**Deliverables**:
- New folder structure in place
- DI container functional
- Exception hierarchy defined
- App.cs registers services

**Testing**:
- Build succeeds
- Add-in loads in Revit
- Manual smoke test of main features

### Phase 2: File Migration (Week 2)
**Goal**: Move files to new locations and update namespaces

**Tasks**:
1. Move infrastructure services (`Services/` → `src/Infrastructure/`)
2. Move domain services to categorized folders
3. Move controllers and commands
4. Move windows/views
5. Split `SharedModels.cs` and `FilterModels.cs` into separate files
6. Update all namespace declarations
7. Update all `using` statements
8. Update `.csproj` file paths

**Deliverables**:
- All files in target locations
- Namespaces updated
- Project builds successfully

**Testing**:
- Build succeeds without errors
- All features work as before
- Deployment successful

### Phase 3: Dependency Injection Integration (Week 3)
**Goal**: Update services to use constructor injection

**Tasks**:
1. Extract interfaces for all services
2. Update service implementations to:
   - Implement interfaces
   - Use constructor injection
   - Validate parameters (ArgumentNullException)
3. Update controllers to use DI
4. Update commands to resolve from container
5. Remove manual service instantiation (`new Service()`)

**Deliverables**:
- All services have interfaces
- All services use constructor injection
- Commands use DI container
- No `new Service()` in business logic

**Testing**:
- Unit test service creation via DI
- Integration test full workflows
- Verify no regression in functionality

### Phase 4: Error Handling (Week 4)
**Goal**: Standardize error handling across the application

**Tasks**:
1. Implement `GlobalErrorHandler`
2. Update all services to throw custom exceptions
3. Add validation methods that throw appropriate exceptions
4. Update controllers to catch and handle exceptions properly
5. Update commands to use `ExecuteWithErrorHandling`
6. Improve user-facing error messages

**Deliverables**:
- Consistent exception throwing
- User-friendly error messages
- Proper error logging
- Graceful error recovery where possible

**Testing**:
- Test error scenarios (null inputs, missing data, API failures)
- Verify error messages are user-friendly
- Verify technical details logged
- No silent failures

### Phase 5: Documentation (Week 5)
**Goal**: Add comprehensive documentation

**Tasks**:
1. Add XML documentation to all public services
2. Add XML documentation to all public models
3. Add XML documentation to all controllers
4. Add XML documentation to all commands
5. Add inline comments for complex algorithms
6. Create architecture documentation
7. Create API documentation
8. Create developer guides

**Deliverables**:
- 100% XML documentation coverage for public members
- Complex algorithms explained with comments
- Architecture documentation complete
- Developer guides available

**Testing**:
- Build with XML documentation warnings enabled
- Code review for documentation quality
- Review documentation for accuracy

### Phase 6: Testing and Validation (Week 6)
**Goal**: Ensure no regressions and validate improvements

**Tasks**:
1. Create unit test project structure
2. Write unit tests for core services
3. Write integration tests
4. Perform manual regression testing
5. Performance testing on large models
6. Update deployment scripts if needed

**Deliverables**:
- Unit test suite with >80% coverage
- Integration test suite
- Regression test pass
- Performance benchmarks met

**Testing**:
- Run full test suite
- Manual testing of all features
- Performance comparison with pre-refactoring

---

## 8. Testing Strategy

### 8.1 Unit Testing Approach

Create test project:

```xml
<!-- Tests/RoomsManagerAddin.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.13.3" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.2.1" />
    <PackageReference Include="Moq" Version="4.18.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\RoomsManagerAddin.csproj" />
  </ItemGroup>
</Project>
```

Example unit tests:

```csharp
// Tests/Unit/Services/Filtering/RoomFilterServiceTests.cs
[TestFixture]
public class RoomFilterServiceTests
{
    private Mock<ILoggingService> _mockLogger;
    private Mock<IRoomParameterDiscoveryService> _mockParameterDiscovery;
    private RoomFilterService _service;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILoggingService>();
        _mockParameterDiscovery = new Mock<IRoomParameterDiscoveryService>();
        _service = new RoomFilterService(null, _mockLogger.Object, _mockParameterDiscovery.Object);
    }

    [Test]
    public void CreateFilterRule_WithValidParameters_CreatesRule()
    {
        // Arrange
        var paramInfo = new ParameterInfo { Name = "Number", DataType = ParameterDataType.Text };
        _mockParameterDiscovery.Setup(x => x.GetParameterByName("Number")).Returns(paramInfo);

        // Act
        var rule = _service.CreateFilterRule("Number", FilterOperator.Equals, "101");

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual("Number", rule.Parameter.Name);
        Assert.AreEqual(FilterOperator.Equals, rule.Operator);
        Assert.AreEqual("101", rule.Value);
    }

    [Test]
    public void CreateFilterRule_WithNullParameter_ThrowsArgumentException()
    {
        // Arrange
        _mockParameterDiscovery.Setup(x => x.GetParameterByName(It.IsAny<string>())).Returns((ParameterInfo)null);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateFilterRule("Invalid", FilterOperator.Equals, "value"));
    }

    [Test]
    public void CreateFilterRule_WithInvalidOperator_ThrowsArgumentException()
    {
        // Arrange
        var paramInfo = new ParameterInfo
        {
            Name = "Number",
            DataType = ParameterDataType.Number
        };
        _mockParameterDiscovery.Setup(x => x.GetParameterByName("Number")).Returns(paramInfo);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateFilterRule("Number", FilterOperator.Contains, "value"));
    }
}
```

### 8.2 Integration Testing

```csharp
// Tests/Integration/RevitApi/ElementCollectorServiceTests.cs
[TestFixture]
public class ElementCollectorServiceTests
{
    // Note: These tests require a running Revit instance with a test document

    private Document _testDocument;
    private ElementCollectorService _service;

    [SetUp]
    public void Setup()
    {
        // Load test document (requires Revit test framework)
        _testDocument = LoadTestDocument("TestModel.rvt");
        _service = new ElementCollectorService();
    }

    [Test]
    public void GetRooms_WithValidDocument_ReturnsRooms()
    {
        // Act
        var rooms = _service.GetRooms(_testDocument);

        // Assert
        Assert.IsNotNull(rooms);
        Assert.Greater(rooms.Count, 0);
        Assert.IsTrue(rooms.All(r => r.Area > 0));
    }
}
```

### 8.3 Manual Testing Checklist

Create comprehensive test plan:

```markdown
# Manual Test Plan

## Test Environment
- Revit 2024
- Test models: Small (10 rooms), Medium (50 rooms), Large (200 rooms)

## Feature Tests

### Room-Wall Analysis
- [ ] Open window successfully
- [ ] Load rooms and walls
- [ ] Apply room filter
- [ ] Configure parameter mappings
- [ ] Run analysis
- [ ] View results
- [ ] Verify parameters updated in Revit

### Filter System
- [ ] Create simple filter (single rule)
- [ ] Create complex filter (nested sets)
- [ ] Test all operators for each parameter type
- [ ] Save and load filter configurations
- [ ] Apply filters to large room sets

### Error Handling
- [ ] Empty document (no rooms)
- [ ] Invalid filter configuration
- [ ] Missing parameters
- [ ] Revit API errors (simulate)
- [ ] File permission errors (read-only log path)

### Performance
- [ ] Small model: < 5 seconds
- [ ] Medium model: < 30 seconds
- [ ] Large model: < 2 minutes
- [ ] Progress reporting works
- [ ] Cancellation works (if implemented)

### Deployment
- [ ] Build succeeds
- [ ] DLL deploys to correct location
- [ ] Manifest file valid
- [ ] Add-in loads in Revit
- [ ] Ribbon panel appears
- [ ] Icons display correctly
```

---

## 9. Risk Mitigation

### 9.1 Identified Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing functionality | Medium | High | Incremental refactoring, extensive testing after each phase |
| Performance regression | Low | Medium | Performance benchmarks before/after, profiling |
| Namespace changes break deployment | Low | High | Test deployment process thoroughly, update documentation |
| DI container bugs | Medium | High | Thorough unit testing of container, use proven patterns |
| Team unfamiliar with new structure | High | Low | Comprehensive documentation, training sessions |

### 9.2 Rollback Plan

If critical issues discovered:

1. **Phase 1-2 Rollback**: Revert file moves, restore original namespaces
2. **Phase 3 Rollback**: Revert DI changes, restore manual instantiation
3. **Phase 4 Rollback**: Revert to original exception handling
4. **Phase 5 Rollback**: Not needed (documentation only)

Maintain `pre-refactoring` Git branch as backup.

### 9.3 Backward Compatibility Assurance

**Code Compatibility**:
- All public APIs remain unchanged
- Existing parameter names preserved
- Filter configurations remain compatible
- Saved settings load correctly

**Build Compatibility**:
- Same .NET Framework version (4.8)
- Same Revit API version (2024)
- Same output DLL name
- Same manifest file format

**Deployment Compatibility**:
- Same installation location
- Same add-in GUID
- Same ribbon panel structure
- Same button commands

---

## 10. Success Criteria

### 10.1 Functional Requirements

- [ ] All existing features work without regression
- [ ] Add-in loads successfully in Revit 2024
- [ ] Room-wall analysis produces same results as before
- [ ] Filter system works with all existing configurations
- [ ] Parameter mapping functions correctly
- [ ] Progress reporting works
- [ ] Error messages displayed correctly

### 10.2 Quality Requirements

- [ ] Code compiles without warnings
- [ ] All services have interfaces
- [ ] All services use constructor injection
- [ ] All public members have XML documentation
- [ ] Complex algorithms have explanatory comments
- [ ] Custom exceptions used consistently
- [ ] Error handling standardized
- [ ] User error messages are friendly and actionable

### 10.3 Structural Requirements

- [ ] Files organized in proper folder structure
- [ ] Namespaces align with folder structure
- [ ] Services categorized by responsibility
- [ ] Models separated by domain
- [ ] Infrastructure separated from domain logic
- [ ] No circular dependencies
- [ ] Clear separation of concerns

### 10.4 Testing Requirements

- [ ] Unit test project created
- [ ] Core services have unit tests (>80% coverage)
- [ ] Integration tests for Revit API services
- [ ] Manual regression tests pass
- [ ] Performance benchmarks met or improved
- [ ] Error scenarios tested

### 10.5 Documentation Requirements

- [ ] All public services documented
- [ ] All public models documented
- [ ] All controllers documented
- [ ] All commands documented
- [ ] Architecture documentation complete
- [ ] Developer guides written
- [ ] CLAUDE.md updated with new structure
- [ ] README updated

### 10.6 Performance Requirements

- [ ] No significant performance regression (<10% slower)
- [ ] DI overhead negligible (<100ms startup time)
- [ ] Memory usage comparable to before
- [ ] Analysis performance targets still met:
  - Small models: < 5 seconds
  - Medium models: < 30 seconds
  - Large models: < 2 minutes

---

## 11. Appendix

### 11.1 Tools and Resources

**Development Tools**:
- Visual Studio 2022
- ReSharper (optional, for refactoring assistance)
- NDepend (optional, for architecture validation)

**Testing Tools**:
- NUnit for unit testing
- Moq for mocking
- Revit Test Framework (for integration tests)

**Documentation Tools**:
- Sandcastle Help File Builder (XML doc → HTML)
- Markdown editors for guides

### 11.2 Reference Links

- .NET Framework 4.8 Documentation
- Revit API 2024 Documentation
- SOLID Principles Guide
- Dependency Injection Patterns
- Clean Architecture by Robert C. Martin

### 11.3 Timeline

| Phase | Duration | Start | End |
|-------|----------|-------|-----|
| Phase 1: Foundation | 1 week | Week 1 | Week 1 |
| Phase 2: File Migration | 1 week | Week 2 | Week 2 |
| Phase 3: DI Integration | 1 week | Week 3 | Week 3 |
| Phase 4: Error Handling | 1 week | Week 4 | Week 4 |
| Phase 5: Documentation | 1 week | Week 5 | Week 5 |
| Phase 6: Testing | 1 week | Week 6 | Week 6 |
| **Total** | **6 weeks** | | |

### 11.4 Team Responsibilities

- **Lead Developer**: Overall refactoring coordination, architecture decisions
- **Developer 1**: File migration, namespace updates
- **Developer 2**: DI implementation, service interfaces
- **Developer 3**: Error handling, exception classes
- **Developer 4**: Documentation, testing
- **QA**: Test plan creation, manual testing, regression verification

---

## 12. Sign-off

This specification has been reviewed and approved:

- [ ] Technical Lead
- [ ] Project Manager
- [ ] QA Lead
- [ ] Stakeholders

**Approved Date**: _______________

**Implementation Start Date**: _______________

**Target Completion Date**: _______________

---

**Document Version**: 1.0
**Last Updated**: 2025-10-18
**Author**: Development Team
**Status**: Draft → Review → Approved → In Progress
