# Testing Strategy - RoomsManagerAddin

**Version**: 2.0 (Post-Refactoring)
**Last Updated**: 2025-10-19

## Overview

This document outlines the testing strategy for RoomsManagerAddin, a Revit 2024 add-in with layered architecture and dependency injection.

## Testing Philosophy

### Priorities

1. **Manual Testing First** - Revit add-ins are inherently difficult to unit test due to Revit API dependencies
2. **Critical Path Coverage** - Focus on Room-Wall analysis, Room-Floor analysis, and parameter mapping
3. **Regression Prevention** - Document test cases to prevent regressions during future changes
4. **Performance Validation** - Ensure acceptable performance (< 30s for 50 rooms analysis)

### Testing Pyramid

```
           ┌─────────────┐
           │   Manual    │  <- Integration with Revit (Primary)
           │   Testing   │
       ┌───┴─────────────┴───┐
       │   Component Testing │  <- Service-level validation
   ┌───┴─────────────────────┴───┐
   │   Unit Testing (Limited)    │  <- DI container, models, filters
   └─────────────────────────────┘
```

**Rationale**: Revit add-ins require real Revit Document/Application context, making traditional unit testing impractical without extensive mocking.

---

## Test Levels

### 1. Unit Tests (Foundational)

**Scope**: Code that doesn't require Revit API

**Testable Components**:
- ✅ **DI Container** (`ServiceContainer`)
  - Singleton lifetime
  - Transient lifetime
  - Constructor injection
  - Error handling

- ✅ **Exception Hierarchy** (`RoomsManagerException`, etc.)
  - Inheritance relationships
  - UserMessage/TechnicalDetails properties
  - Exception catching by base type

- ✅ **Filter Models** (`FilterOperator`, `FilterSet`, `RoomFilterRule`)
  - Enum values
  - Model creation
  - Nested filter sets
  - Logical operators (AND/OR)

**Implementation Status**: ✅ Test code created (see `specs/main/checklists/unit-test-samples.md`)

**Note**: Full unit test execution requires standalone test project with Revit API mocking framework (not implemented in Phase 6).

---

### 2. Component Tests (Service-Level)

**Scope**: Individual services with mocked dependencies

**Testable Services**:

#### Filtering Services
- `RoomFilterService.GetAvailableParameters()` - Should return list of parameter metadata
- `RoomFilterService.CreateFilterRule()` - Should create valid filter rules
- `RoomFilterService.ValidateFilterSet()` - Should catch invalid operators for parameter types
- `RoomFilterService.ApplyFilter()` - Should filter rooms correctly (requires Revit Document)

**Expected Behavior**:
```csharp
// Pseudocode example
var mockDocument = CreateMockDocument(withRooms: 10);
var filterService = new RoomFilterService(mockDocument, mockLogging);

var config = filterService.CreateFilterConfiguration("Test");
var rule = filterService.CreateFilterRule("Area", FilterOperator.GreaterThan, "100");
config.RootFilterSet.Items.Add(rule);

var matchingRooms = filterService.ApplyFilter(config);
Assert.AreEqual(expectedCount, matchingRooms.Count);
```

**Challenge**: Requires Revit API mocking (Document, Room, Parameter objects).

**Implementation Status**: ⏸️ Deferred - Requires Revit Test Framework

---

### 3. Integration Tests (Revit Context)

**Scope**: Full workflows with real Revit Document

**Critical Workflows**:

#### Room-Wall Analysis
- ✅ Load rooms and walls from document
- ✅ Apply level/area filters
- ✅ Run Room Boundary API analysis
- ✅ Verify collision counts
- ✅ Execute parameter mappings
- ✅ Verify Revit parameters updated

#### Room-Floor Analysis
- ✅ Load rooms and floors from document
- ✅ Run solid intersection analysis
- ✅ Handle multi-level rooms
- ✅ Verify floor detection accuracy

#### Advanced Filtering
- ✅ Create simple filter (Area > 100)
- ✅ Create AND filter (Area > 100 AND Level = "Level 1")
- ✅ Create OR filter (Area > 200 OR Volume > 1000)
- ✅ Create nested filter ((A AND B) OR C)
- ✅ Validate filter preview counts
- ✅ Apply filter and verify results

**Implementation**: Manual testing with test Revit models (see `specs/main/checklists/manual-testing-checklist.md`)

---

### 4. Manual Testing (Primary Validation)

**Test Artifacts**:
- ✅ **Manual Testing Checklist** - `specs/main/checklists/manual-testing-checklist.md`
  - 100+ test cases
  - Organized by feature area
  - Pass/Fail tracking
  - Performance benchmarks

**Test Model Requirements**:
- 10-50 rooms (varying areas: 50-500 sq ft)
- 20-200 walls (different types)
- 5-100 floors (different types)
- Multiple levels (2-3)
- Shared parameters configured

**Key Test Scenarios**:
1. ✅ Basic room-wall analysis (< 5 seconds for 10 rooms)
2. ✅ Basic room-floor analysis (< 30 seconds for 10 rooms)
3. ✅ Level filtering
4. ✅ Area filtering
5. ✅ Advanced filtering (complex AND/OR rules)
6. ✅ Parameter mapping (Room→Wall, Wall→Room, Bidirectional)
7. ✅ Error handling (no selection, invalid data, Revit API errors)
8. ✅ Performance (50 rooms + 200 walls < 30 seconds)
9. ✅ UI/UX (window behavior, progress reporting, list scrolling)
10. ✅ Logging (file creation, content, error logging)

---

## Test Environment Setup

### Prerequisites

1. **Revit 2024** installed
2. **Test Revit Model** prepared with rooms, walls, floors
3. **Shared Parameters** (optional - for parameter mapping tests)
4. **Log File Location** access (Desktop or SaveDialog)

### Deployment for Testing

**Standard Deployment**:
```powershell
# Build
dotnet build RoomsManagerAddin.csproj --configuration Debug

# Deploy
powershell -Command "Copy-Item 'bin\\Debug\\net48\\RoomsManagerAddin.dll' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"
powershell -Command "Copy-Item 'RoomsManagerAddin.addin' 'C:\\Users\\$env:USERNAME\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2024\\'"

# Restart Revit
```

**Verify Deployment**:
- Check DLL timestamp matches build time
- Verify add-in appears in ribbon
- Check log file for any startup errors

---

## Performance Benchmarks

### Target Performance

| Operation | Model Size | Target Time | Acceptable Time |
|-----------|-----------|-------------|-----------------|
| Room-Wall Analysis | 10 rooms, 20 walls | < 2 seconds | < 5 seconds |
| Room-Wall Analysis | 50 rooms, 200 walls | < 10 seconds | < 30 seconds |
| Room-Floor Analysis | 10 rooms, 5 floors | < 15 seconds | < 30 seconds |
| Room-Floor Analysis | 50 rooms, 100 floors | < 30 seconds | < 60 seconds |
| Complex Filter | 1000 rooms | < 500ms | < 1 second |
| Parameter Mapping | 50 rooms, 200 walls | < 2 seconds | < 5 seconds |

### Performance Testing Process

1. Prepare test model with known element counts
2. Run analysis 3 times (to account for variability)
3. Record average time
4. Compare against benchmarks
5. If exceeds acceptable time, investigate performance bottlenecks

**Tools**:
- Revit's built-in performance profiler (if available)
- Log file timestamps (start/end analysis)
- Stopwatch for manual timing

---

## Regression Testing

### When to Run Regression Tests

- ✅ After major refactoring (like Phase 2-5)
- ✅ Before version releases
- ✅ After adding new features
- ✅ After fixing critical bugs

### Regression Test Suite

**Minimal Suite** (30 minutes):
1. Load rooms and walls
2. Run basic room-wall analysis
3. Verify collision counts
4. Apply simple filter (Area > 100)
5. Execute parameter mapping (Room→Wall)
6. Check log file for errors

**Complete Suite** (2 hours):
- Follow full manual testing checklist (`specs/main/checklists/manual-testing-checklist.md`)

---

## Test Data Management

### Test Model Storage

**Location**: `tests/TestModels/` (not in repository due to size)

**Test Models**:
1. `SmallModel.rvt` - 10 rooms, 20 walls, 5 floors
2. `MediumModel.rvt` - 50 rooms, 200 walls, 100 floors
3. `LargeModel.rvt` - 200 rooms, 1000 walls, 500 floors (performance testing)

**Model Preparation**:
- Use consistent naming: Room 1, Room 2, etc.
- Assign rooms to specific levels
- Vary room areas: 50, 100, 200, 500 sq ft
- Include some rooms with Area = 0 (for filtering tests)
- Configure shared parameters for parameter mapping tests

---

## Known Testing Limitations

### What Cannot Be Easily Tested

1. **Revit API Behavior**
   - Revit's Room.GetBoundarySegments() internal logic
   - Solid intersection accuracy
   - Parameter update transactions

2. **UI Rendering**
   - WPF window appearance
   - Control styling
   - Icon display

3. **Multi-Threading**
   - Revit API is single-threaded
   - Cannot test concurrent operations

### Workarounds

- **Manual Inspection**: For UI and rendering
- **Log File Analysis**: For transaction and API behavior
- **Real-World Testing**: With actual project files

---

## Test Documentation

### Test Case Template

```markdown
**Test Case**: TC-001 - Room-Wall Analysis Basic

**Objective**: Verify room-wall analysis completes successfully

**Preconditions**:
- Revit 2024 open with test model
- At least 10 rooms and 20 walls in model

**Steps**:
1. Open RoomDataSync window
2. Click "Load Rooms and Walls"
3. Select all rooms
4. Select all walls
5. Click "Run Analysis"

**Expected Results**:
- Analysis completes without errors
- Progress window shows stages: Collecting, Analyzing, Mapping
- Results show collision counts > 0
- Log file created with detailed mappings

**Actual Results**: [To be filled by tester]

**Status**: PASS / FAIL

**Notes**: [Any observations or issues]
```

### Test Results Tracking

**Location**: `specs/main/checklists/test-results-YYYYMMDD.md`

**Format**:
- Date and tester name
- Test environment (Revit version, OS, model used)
- Test cases executed
- Pass/Fail status
- Issues found
- Performance measurements

---

## Continuous Improvement

### Metrics to Track

1. **Test Coverage** (manual)
   - Features tested vs. total features
   - Critical paths vs. edge cases

2. **Defect Density**
   - Bugs found per release
   - Bugs by severity

3. **Performance Trends**
   - Analysis time over versions
   - Memory usage

### Feedback Loop

1. User reports issue →
2. Add test case to manual checklist →
3. Fix issue →
4. Verify with test case →
5. Include in regression suite

---

## Future Testing Enhancements

### Phase 7+ (Future)

1. **Revit Test Framework Integration**
   - Setup Revit Test Runner
   - Create automated integration tests
   - Run tests in CI/CD pipeline

2. **Unit Test Coverage for Core**
   - DI container full test suite
   - Filter validation logic
   - Exception hierarchy

3. **Performance Profiling**
   - Identify bottlenecks
   - Optimize Room Boundary API usage
   - Reduce solid intersection time

4. **Automated UI Testing**
   - WPF UI automation
   - Screenshot comparison
   - Accessibility testing

---

## Appendix A: Sample Unit Test Code

For reference, sample unit test code has been created for:
- `ServiceContainerTests.cs` - DI container validation
- `ExceptionHierarchyTests.cs` - Exception behavior
- `FilterOperatorTests.cs` - Filter models and operators

**Location**: See `specs/main/checklists/` directory for test code samples.

**Note**: These tests are reference implementations and require a standalone test project to execute.

---

## Appendix B: Testing Tools

### Recommended Tools

1. **MSTest** - Microsoft's unit testing framework (for future unit tests)
2. **Moq** - Mocking framework (for mocking Revit API)
3. **Revit Test Framework** - Official Revit testing tool (complex setup)
4. **Stopwatch** - Manual performance timing
5. **Revit API Docs** - For understanding expected behavior

### Tool Setup (Future)

```xml
<!-- Example: Test project packages -->
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  <PackageReference Include="MSTest.TestAdapter" Version="3.1.1" />
  <PackageReference Include="MSTest.TestFramework" Version="3.1.1" />
  <PackageReference Include="Moq" Version="4.20.70" />
</ItemGroup>
```

---

**Document Version**: 1.0
**Last Reviewed**: 2025-10-19
**Next Review**: Before Phase 7 (Future Enhancements)
