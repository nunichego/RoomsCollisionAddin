# RoomsManagerAddin Constitution

**Project**: Rooms Manager Add-in for Revit 2024
**Framework**: .NET Framework 4.8
**Purpose**: Room collision detection, spatial analysis, and parameter synchronization
**Last Updated**: 2025-10-18

---

## 1. Core Principles

### 1.1 Service-Oriented Architecture
- **All business logic MUST be encapsulated in specialized services**
  - Services are single-responsibility, testable components
  - Each service handles one specific domain (geometry, filtering, collision analysis, etc.)
  - Services must be stateless where possible, accepting dependencies via constructor injection

- **Controllers orchestrate services and manage workflow**
  - Controllers coordinate between multiple services
  - Controllers handle UI-to-business-logic translation
  - Controllers must NOT contain business logic themselves

- **Commands serve as thin entry points**
  - IExternalCommand implementations must be minimal
  - Commands instantiate controllers and open UI
  - Commands handle only Revit API lifecycle concerns

### 1.2 Dependency Injection Pattern
- **Constructor injection is the standard pattern**
  - All service dependencies must be injected via constructor
  - Avoid new keyword for service instantiation within services
  - Make dependencies explicit and testable

- **Document references must be injected**
  - Never access static Revit application context within services
  - Document instances passed from commands through controllers to services

### 1.3 Separation of Concerns
- **UI code (XAML/WPF) must be isolated from business logic**
  - Windows/Forms handle only presentation and user interaction
  - All computation delegated to controllers and services
  - Data binding preferred over manual UI updates

- **Revit API access must be centralized**
  - ElementCollectorService for all element collection
  - GeometryService for all geometric calculations
  - Services wrap Revit API complexity with clear abstractions

---

## 2. Code Quality Standards

### 2.1 Naming Conventions
- **Services**: `<Domain>Service` (e.g., `CollisionAnalysisService`, `RoomFilterService`)
- **Controllers**: `<Feature>Controller` (e.g., `RoomWallAnalysisController`)
- **Commands**: `<Action>Command` (e.g., `RoomDataSyncCommand`)
- **Models**: Descriptive nouns (e.g., `RoomCollisionResult`, `FilterConfiguration`)
- **Interfaces**: `I<Name>` prefix (e.g., `IConfigurationService`)

### 2.2 Documentation Requirements
- **All public methods MUST have XML documentation comments**
  - Include `<summary>` describing purpose
  - Include `<param>` for all parameters
  - Include `<returns>` for non-void methods
  - Include `<exception>` for thrown exceptions

- **Complex algorithms MUST have inline comments**
  - Explain WHY, not just WHAT
  - Document performance considerations
  - Note Revit API quirks and workarounds

- **Services MUST have class-level documentation**
  - Describe service responsibility
  - Document dependencies
  - Note usage patterns and examples

### 2.3 Error Handling
- **All Revit API calls MUST be wrapped in try-catch blocks**
  - Catch specific exceptions where possible
  - Provide user-friendly error messages
  - Log technical details for debugging

- **Services MUST validate input parameters**
  - Throw ArgumentNullException for null required parameters
  - Throw ArgumentException for invalid values
  - Validate early, fail fast

- **Error messages MUST be actionable**
  - Tell users what went wrong
  - Suggest how to fix the problem
  - Avoid technical jargon in UI messages

### 2.4 Logging Standards
- **All services MUST accept a logging callback or service**
  - Use `Action<string> writeToLog` pattern or `LoggingService`
  - Log method entry/exit for complex operations
  - Log performance metrics for long-running operations

- **Log levels:**
  - **Critical**: Operation failures that prevent core functionality
  - **Summary**: High-level operation results (counts, timing)
  - **Debug**: Detailed information for troubleshooting (disabled in production)
  - **Never log per-element details in production** (causes performance degradation)

- **Performance logging requirements:**
  - Log start time for operations > 1 second expected duration
  - Log completion time and duration
  - Log element counts processed

---

## 3. Testing Standards

### 3.1 Unit Testing Requirements
- **All services MUST be unit testable**
  - Services must not depend on Revit context
  - Use interfaces for external dependencies
  - Write tests for business logic and validation rules

- **Test naming convention**: `<MethodName>_<Scenario>_<ExpectedResult>`
  - Example: `ApplyFilter_WithValidConfiguration_ReturnsFilteredRooms`

- **Test coverage targets:**
  - All public service methods: 100%
  - Complex algorithms: 100%
  - Error handling paths: 100%
  - UI code: Best effort (WPF testing is complex)

### 3.2 Integration Testing
- **Test with real Revit documents when possible**
  - Create minimal test models for specific scenarios
  - Test on models of varying complexity (small, medium, large)
  - Validate performance targets on representative models

### 3.3 Manual Testing Checklist
- **Before every release:**
  - Test all UI workflows end-to-end
  - Test with empty documents
  - Test with complex documents (100+ rooms, 500+ walls)
  - Test error scenarios (invalid parameters, null selections)
  - Verify deployment process

---

## 4. User Experience Consistency

### 4.1 WPF/XAML Standards
- **Use provided templates for all windows**
  - `ModernWindowTemplate.xaml` for complex dialogs
  - `SimpleWindowTemplate.xaml` for simple dialogs
  - Maintain consistent styling across all windows

- **Window sizing and behavior:**
  - Set reasonable default sizes (800x600 for complex, 400x300 for simple)
  - Make windows resizable where content benefits
  - Remember window positions if feasible

- **Color scheme and fonts:**
  - Use theme-defined colors from templates
  - Use system fonts (Segoe UI on Windows)
  - Ensure adequate contrast for readability

### 4.2 Progress Reporting
- **All long-running operations MUST show progress**
  - Use `ProgressReporter` for operations > 2 seconds
  - Report percentage and current operation
  - Allow cancellation where feasible

- **Progress window requirements:**
  - Use `ModernProgressWindow` for consistency
  - Show percentage progress bar
  - Show current step description
  - Display elapsed time for operations > 10 seconds

### 4.3 Error Messages and Dialogs
- **Error message structure:**
  - **Title**: Brief problem description
  - **Body**: What went wrong and why
  - **Action**: What user should do next

- **Example:**
  ```
  Title: "No Rooms Found"
  Body: "The current document contains no placed rooms. Rooms are required for collision analysis."
  Action: "Please place rooms in the model and try again."
  ```

- **Use appropriate dialog types:**
  - Error: Critical failures
  - Warning: Potential issues user should know about
  - Information: Success confirmations, helpful tips

### 4.4 Data Grids and Lists
- **All data grids MUST be sortable**
  - Enable column sorting on all data columns
  - Provide clear visual indicators for sort direction

- **Support filtering where appropriate**
  - Provide text search for large lists
  - Support advanced filtering for complex data

- **Selection behavior:**
  - Support multi-select for batch operations
  - Preserve selection after operations
  - Provide "Select All" / "Clear Selection" buttons

---

## 5. Performance Requirements

### 5.1 Analysis Performance Targets
- **Room-Wall collision analysis:**
  - Small models (<50 rooms, <200 walls): < 5 seconds
  - Medium models (50-200 rooms, 200-1000 walls): < 30 seconds
  - Large models (200+ rooms, 1000+ walls): < 2 minutes

- **If targets not met, investigate:**
  - Pre-computation opportunities
  - Spatial filtering optimizations
  - Algorithm complexity reduction

### 5.2 Optimization Strategies
- **Pre-compute expensive operations**
  - Calculate wall solids once, reuse for all rooms
  - Pre-calculate bounding boxes for spatial filtering
  - Cache parameter values when accessing multiple times

- **Use multi-stage filtering:**
  1. Fast spatial filtering (Z-axis, bounding box)
  2. Moderate filtering (parameter checks)
  3. Expensive operations last (solid intersection)

- **Profile before optimizing**
  - Measure operation timing with logging
  - Identify bottlenecks with evidence
  - Optimize highest-impact operations first

### 5.3 Memory Management
- **Dispose of Revit API objects properly**
  - Use `using` statements for IDisposable objects
  - Don't hold references to elements longer than necessary
  - Clear large collections after processing

- **Avoid memory leaks:**
  - Unsubscribe from events when done
  - Clear event handlers on window close
  - Don't cache entire element collections

### 5.4 UI Responsiveness
- **Never block UI thread for > 1 second**
  - Use background threads for long operations
  - Update progress on UI thread periodically
  - Allow cancellation of long operations

- **Provide immediate feedback**
  - Change cursor to wait cursor during operations
  - Disable buttons during processing
  - Re-enable UI elements when complete

---

## 6. Revit API Best Practices

### 6.1 Transaction Management
- **All element modifications MUST be in transactions**
  - Start transaction before any modification
  - Use descriptive transaction names
  - Commit on success, rollback on error

- **Transaction scope:**
  - Keep transactions as short as possible
  - Don't include UI operations in transactions
  - Group related modifications in single transaction

- **Example pattern:**
  ```csharp
  using (Transaction trans = new Transaction(document, "Update Room Parameters"))
  {
      trans.Start();
      try
      {
          // Modify elements here
          trans.Commit();
      }
      catch (Exception ex)
      {
          trans.RollBack();
          throw;
      }
  }
  ```

### 6.2 Element Collection
- **Use FilteredElementCollector efficiently**
  - Apply filters to reduce elements collected
  - Use OfClass or OfCategory filters first
  - Chain filters from most to least restrictive

- **Avoid ToElements() unless necessary**
  - Use ToElementIds() when only IDs needed
  - Filter further before converting to elements

### 6.3 Parameter Access
- **Cache parameter lookups**
  - LookupParameter is expensive, cache results
  - Store ElementId for shared parameters
  - Validate parameter exists before accessing

- **Handle missing parameters gracefully**
  - Check if parameter exists before reading
  - Provide default values for missing parameters
  - Don't crash on missing parameters

### 6.4 Geometry Operations
- **Geometry extraction is expensive**
  - Extract geometry once per element
  - Use Options to control detail level
  - Prefer fast methods (e.g., `room.get_Geometry()`) over expensive ones (SEGC)

- **Solid operations are very expensive**
  - Minimize Boolean operations
  - Filter candidates before solid intersection
  - Consider bounding box checks first

---

## 7. Deployment and Build Standards

### 7.1 Build Process
- **Always use `dotnet build` command**
  - Never use `msbuild` directly
  - Build for Debug or Release configuration explicitly
  - Target framework: `net48`

- **Build command:**
  ```bash
  dotnet build RoomsManagerAddin.csproj --configuration Debug
  dotnet build RoomsManagerAddin.csproj --configuration Release
  ```

### 7.2 Deployment Process
- **Use PowerShell deployment commands**
  - Copy from `bin\Debug\net48\` or `bin\Release\net48\` directory
  - Copy both DLL and .addin manifest
  - Verify deployment with timestamp check

- **Deployment commands:**
  ```powershell
  # Copy DLL
  Copy-Item 'bin\Debug\net48\RoomsManagerAddin.dll' 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\'

  # Copy manifest
  Copy-Item 'RoomsManagerAddin.addin' 'C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2024\'
  ```

### 7.3 Version Control
- **NEVER commit:**
  - bin/ or obj/ directories
  - .vs/ directory
  - User-specific files (*.user)
  - Build artifacts

- **ALWAYS commit:**
  - Source code (.cs files)
  - Project files (.csproj)
  - Manifest files (.addin)
  - Resources (icons, templates)
  - Documentation (*.md files)

---

## 8. Documentation Requirements

### 8.1 Project Documentation
- **MUST update after significant changes:**
  - `Resources/documents/history.md` - Feature additions, optimizations, architectural changes
  - `Resources/documents/errors_history.md` - Errors encountered and solutions
  - `CLAUDE.md` - Development guidelines and build instructions

### 8.2 Documentation Format
- **history.md structure:**
  - Chronological entries by date
  - Include problem description, solution, and results
  - Document performance improvements with metrics
  - Maintain "Current Status" and "Next Steps" sections

- **errors_history.md structure:**
  - Error description with context
  - Root cause analysis
  - Solution implemented
  - Prevention strategies

### 8.3 Code Comments
- **When to comment:**
  - Complex algorithms (explain approach)
  - Revit API workarounds (explain why)
  - Performance optimizations (explain trade-offs)
  - Non-obvious business rules

- **When NOT to comment:**
  - Self-explanatory code
  - Variable declarations (use descriptive names instead)
  - Obvious method calls

---

## 9. Task Clarification Protocol

### 9.1 Before Implementation
- **ALWAYS ask 3 clarifying questions for change requests:**
  1. **Scope**: What specific components/files should be modified?
  2. **Implementation**: How should the change be implemented technically?
  3. **Impact**: What are the expected outcomes or side effects?

### 9.2 Question Examples
- "Which service should handle this new functionality?"
- "Should this be a new method or modify existing logic?"
- "How should this affect the existing UI/workflow?"
- "What performance considerations should be taken into account?"
- "Should this be backwards compatible?"

### 9.3 Purpose
- Prevent misunderstandings and incorrect implementations
- Ensure changes align with expectations
- Identify potential conflicts or dependencies early
- Maintain code quality and architectural consistency

---

## 10. Filter System Standards

### 10.1 Filter Architecture
- **RoomFilterService handles all filtering logic**
  - Supports complex rule configurations
  - Validates rules before application
  - Provides real-time match count

- **Filter components:**
  - **FilterRule**: Single parameter-based condition
  - **FilterSet**: Logical grouping with AND/OR operators
  - **FilterConfiguration**: Complete filter definition

### 10.2 Filter Rules
- **Supported operators by parameter type:**
  - **Text**: Equals, NotEquals, Contains, NotContains, StartsWith, EndsWith, IsEmpty, IsNotEmpty
  - **Number**: Equals, NotEquals, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
  - **Boolean**: Equals, NotEquals

- **Rule validation:**
  - Verify parameter exists
  - Verify operator is valid for parameter type
  - Validate value format matches parameter type

### 10.3 Nested Logic
- **FilterSets can contain other FilterSets**
  - Enables complex conditions: `(A AND B) OR (C AND D)`
  - Maximum nesting depth: 5 levels (prevent over-complexity)
  - Validate logical structure before application

---

## 11. Change Management

### 11.1 Feature Addition Process
1. **Design Phase**
   - Document requirements
   - Identify affected services
   - Design API and data structures
   - Ask clarifying questions

2. **Implementation Phase**
   - Create/modify services with tests
   - Update controllers
   - Implement UI changes
   - Update documentation

3. **Testing Phase**
   - Unit test new services
   - Integration test with Revit
   - Manual test all workflows
   - Performance test on large models

4. **Documentation Phase**
   - Update `history.md`
   - Update `CLAUDE.md` if needed
   - Update XML documentation
   - Update README if applicable

### 11.2 Bug Fix Process
1. **Reproduce and document**
   - Capture exact steps to reproduce
   - Document expected vs actual behavior
   - Note any error messages

2. **Root cause analysis**
   - Identify the failing component
   - Understand why it fails
   - Determine scope of impact

3. **Fix and validate**
   - Implement minimal fix
   - Add test to prevent regression
   - Verify fix doesn't break other functionality

4. **Document in errors_history.md**
   - Describe error and context
   - Explain root cause
   - Document solution and prevention

### 11.3 Refactoring Guidelines
- **When to refactor:**
  - Code duplication (DRY principle violated)
  - Method too long (> 50 lines)
  - Class too large (> 500 lines)
  - Poor performance identified

- **When NOT to refactor:**
  - If it ain't broke, don't fix it
  - Before understanding the system
  - Without tests to verify correctness

---

## 12. Summary and Enforcement

### 12.1 Critical Rules (MUST follow)
1. All business logic in services
2. Constructor injection for dependencies
3. All public methods have XML documentation
4. All Revit API calls in try-catch blocks
5. Log performance metrics for long operations
6. Use PowerShell deployment (not Install.bat)
7. Update documentation after significant changes
8. Ask 3 clarifying questions before implementing changes

### 12.2 High-Priority Rules (SHOULD follow)
1. Unit test all services
2. Use provided WPF templates
3. Show progress for operations > 2 seconds
4. Meet performance targets
5. Cache expensive Revit API lookups
6. Use multi-stage filtering strategies

### 12.3 Best Practices (RECOMMENDED)
1. Profile before optimizing
2. Use descriptive variable names over comments
3. Keep methods focused and concise
4. Prefer composition over inheritance
5. Write self-documenting code

---

**This constitution is a living document and should be updated as the project evolves and new patterns emerge.**
