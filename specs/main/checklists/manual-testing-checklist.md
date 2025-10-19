# Manual Testing Checklist - RoomsManagerAddin

**Version**: 2.0 (Post-Refactoring)
**Date**: 2025-10-19
**Tester**: _________________

## Pre-Testing Setup

- [ ] Build project in Debug configuration
- [ ] Deploy DLL and .addin file to Revit 2024 addins folder
- [ ] Verify deployment timestamp matches build time
- [ ] Open Revit 2024
- [ ] Verify "AH RoomsDataSync (Demo)" panel appears in ribbon
- [ ] Prepare test Revit model with rooms, walls, and floors

## Test Model Requirements

**Minimum Test Data**:
- [ ] At least 10 rooms with varying areas (50-500 sq ft)
- [ ] At least 20 walls (different types)
- [ ] At least 5 floors (different types)
- [ ] Rooms on at least 2 different levels
- [ ] Some rooms with shared parameters
- [ ] Some rooms without Area (should be filtered out)

---

## 1. Basic Functionality Tests

### 1.1 Add-in Loading
- [ ] Add-in loads without errors
- [ ] Ribbon panel displays correctly
- [ ] All three buttons visible: "RoomsMapping", "Settings", "Help"
- [ ] Button icons display correctly

### 1.2 Command Execution
- [ ] "RoomsMapping" button opens main window
- [ ] Window displays without errors
- [ ] Window is properly sized and positioned
- [ ] All UI elements are visible

---

## 2. Room-Wall Analysis Tests

### 2.1 Basic Analysis
- [ ] Click "Load Rooms and Walls"
- [ ] Rooms list populates (count: _______)
- [ ] Walls list populates (count: _______)
- [ ] Room items show: Number, Name, Area, Level
- [ ] Wall items show: Id, Type, Level
- [ ] Select all rooms and walls
- [ ] Click "Run Analysis"
- [ ] Progress window appears
- [ ] Analysis completes without errors
- [ ] Results display collision counts
- [ ] Log file is created (Desktop or SaveDialog location)

**Expected Results**:
- Analysis time for 10 rooms + 20 walls: < 5 seconds
- Log file contains detailed room-wall mappings
- No crashes or Revit errors

### 2.2 Level Filtering
- [ ] Select specific level from "Room Level Filter" dropdown
- [ ] Click "Apply Filters"
- [ ] Only rooms on selected level remain visible
- [ ] Wall level filter works independently
- [ ] Clear filters restores all items

### 2.3 Area Filtering
- [ ] Enter minimum area (e.g., "100")
- [ ] Click "Apply Filters"
- [ ] Only rooms with area >= 100 remain
- [ ] Combine with level filter
- [ ] Both filters work together correctly

---

## 3. Room-Floor Analysis Tests

### 3.1 Basic Floor Analysis
- [ ] Click "Load Rooms and Floors"
- [ ] Floors list populates (count: _______)
- [ ] Select rooms and floors
- [ ] Click "Run Floor Analysis"
- [ ] Analysis completes (may take 10-30 seconds for 10 rooms)
- [ ] Results show floor collision counts
- [ ] Log file contains floor analysis details

**Expected Results**:
- Analysis time for 10 rooms + 5 floors: < 30 seconds
- Accurate floor detection for multi-level rooms
- No geometry errors

---

## 4. Advanced Filtering Tests

### 4.1 Simple Filter Rule
- [ ] Click "Advanced Filter" button
- [ ] Select parameter: "Area"
- [ ] Select operator: "GreaterThan"
- [ ] Enter value: "100"
- [ ] Click "Add Rule"
- [ ] Rule appears in filter tree
- [ ] Click "Preview" - shows matching room count
- [ ] Click "Apply Filter"
- [ ] Rooms list updates to show only matches

### 4.2 Complex Filter (AND Logic)
- [ ] Create filter: Area > 100 AND Level = "Level 1"
- [ ] Set root operator to "AND"
- [ ] Add both rules
- [ ] Preview shows correct count
- [ ] Apply filter
- [ ] Verify only rooms matching BOTH conditions remain

### 4.3 Complex Filter (OR Logic)
- [ ] Create filter: Area > 200 OR Volume > 1000
- [ ] Set root operator to "OR"
- [ ] Add both rules
- [ ] Preview shows correct count
- [ ] Apply filter
- [ ] Verify rooms matching EITHER condition remain

### 4.4 Nested Filter Sets
- [ ] Create: (Area > 100 AND Level = "Level 1") OR (Volume > 1000)
- [ ] Create child FilterSet with AND operator
- [ ] Add Area and Level rules to child
- [ ] Add Volume rule to root
- [ ] Set root operator to OR
- [ ] Preview and verify logic
- [ ] Apply filter successfully

### 4.5 Filter Validation
- [ ] Try invalid value for numeric parameter (e.g., "abc" for Area)
- [ ] Error message appears
- [ ] Try empty filter configuration
- [ ] Appropriate validation message shown

---

## 5. Parameter Mapping Tests

### 5.1 Room → Wall Mapping
- [ ] Configure mapping: Room "Name" → Wall "Comments"
- [ ] Set direction: RoomToElement
- [ ] Run analysis with mapping enabled
- [ ] Open wall properties in Revit
- [ ] Verify "Comments" parameter contains room name
- [ ] Check multiple walls

### 5.2 Wall → Room Mapping
- [ ] Configure mapping: Wall "Type Name" → Room custom parameter
- [ ] Set direction: ElementToRoom
- [ ] Run analysis
- [ ] Open room properties in Revit
- [ ] Verify parameter updated with wall type name

### 5.3 Bidirectional Mapping
- [ ] Configure both Room→Wall and Wall→Room mappings
- [ ] Run analysis
- [ ] Verify both directions executed correctly
- [ ] Check log file for mapping confirmation

---

## 6. Error Handling Tests

### 6.1 No Selection Errors
- [ ] Try running analysis with no rooms selected
- [ ] User-friendly error message appears
- [ ] Try with no walls selected
- [ ] Appropriate error message shown

### 6.2 Invalid Data Handling
- [ ] Select room with no area (area = 0)
- [ ] Room should be automatically filtered out
- [ ] Try analysis with only invalid rooms
- [ ] Appropriate error/warning shown

### 6.3 Revit API Errors
- [ ] Close document during analysis (if possible)
- [ ] Error handled gracefully
- [ ] No Revit crash
- [ ] User-friendly error message

---

## 7. Performance Tests

### 7.1 Small Model Performance
**Test Data**: 10 rooms, 20 walls

- [ ] Run room-wall analysis
- [ ] Record time: _________ seconds
- [ ] Expected: < 5 seconds
- [ ] PASS / FAIL

### 7.2 Medium Model Performance
**Test Data**: 50 rooms, 200 walls

- [ ] Run room-wall analysis
- [ ] Record time: _________ seconds
- [ ] Expected: < 30 seconds
- [ ] PASS / FAIL

### 7.3 Floor Analysis Performance
**Test Data**: 50 rooms, 100 floors

- [ ] Run room-floor analysis
- [ ] Record time: _________ seconds
- [ ] Expected: < 60 seconds (solid intersection is slower)
- [ ] PASS / FAIL

### 7.4 Complex Filter Performance
**Test Data**: 1000 rooms (if available)

- [ ] Create complex nested filter
- [ ] Apply filter
- [ ] Record time: _________ seconds
- [ ] Expected: < 1 second (in-memory evaluation)
- [ ] PASS / FAIL

---

## 8. Logging and Debugging Tests

### 8.1 Log File Creation
- [ ] Run analysis
- [ ] Log file created at expected location
- [ ] File opens successfully
- [ ] Contains timestamp header
- [ ] Contains room-wall mapping details

### 8.2 Log File Content
- [ ] Log shows analysis start time
- [ ] Log shows each room processed
- [ ] Log shows collision counts
- [ ] Log shows parameter mapping execution
- [ ] Log shows completion time
- [ ] No unexpected errors in log

### 8.3 Error Logging
- [ ] Trigger an error (e.g., invalid filter)
- [ ] Error logged with [ERROR] prefix
- [ ] Technical details included in log
- [ ] User message appropriate

---

## 9. UI/UX Tests

### 9.1 Window Behavior
- [ ] Window can be resized
- [ ] Window can be moved
- [ ] Window stays on top of Revit
- [ ] Close button works
- [ ] Window reopens correctly

### 9.2 Progress Reporting
- [ ] Progress window appears during analysis
- [ ] Progress messages update in real-time
- [ ] Progress shows stages: "Collecting", "Analyzing", "Mapping"
- [ ] Progress window closes automatically on completion

### 9.3 List/Grid Behavior
- [ ] Rooms list scrolls correctly
- [ ] Walls list scrolls correctly
- [ ] Multi-select works (Ctrl+Click, Shift+Click)
- [ ] Select All checkbox works
- [ ] Column headers visible

---

## 10. Dependency Injection Tests

### 10.1 Service Resolution
- [ ] All services resolve without errors (check log for DI errors)
- [ ] No circular dependency errors
- [ ] Singleton services reused (e.g., LoggingService)
- [ ] Transient services created per-request

### 10.2 Error Handling via DI
- [ ] GlobalErrorHandler initialized correctly
- [ ] Errors caught and logged via centralized handler
- [ ] User messages appropriate (not technical stack traces)

---

## 11. Regression Tests (Previous Functionality)

### 11.1 Room Boundary API
- [ ] Room-wall analysis uses Room Boundary API
- [ ] Fast performance (< 5 seconds for 50 rooms)
- [ ] Accurate wall detection
- [ ] Log confirms "Room Boundary API" usage

### 11.2 Solid Intersection
- [ ] Room-floor analysis uses solid intersection
- [ ] Accurate floor detection
- [ ] Handles multi-level rooms
- [ ] Log confirms "Solid Intersection" usage

---

## 12. Deployment and Installation Tests

### 12.1 Fresh Installation
- [ ] Delete old DLL and .addin files
- [ ] Deploy fresh build
- [ ] Restart Revit
- [ ] Add-in loads correctly
- [ ] No errors on first run

### 12.2 Update Installation
- [ ] Keep old DLL in place
- [ ] Deploy new build
- [ ] Restart Revit
- [ ] New version loads (verify timestamp or version)
- [ ] No conflicts with old files

---

## Test Results Summary

**Total Tests**: _______
**Passed**: _______
**Failed**: _______
**Blocked**: _______

### Critical Issues Found

1. ______________________________________________
2. ______________________________________________
3. ______________________________________________

### Non-Critical Issues Found

1. ______________________________________________
2. ______________________________________________
3. ______________________________________________

### Performance Notes

- ______________________________________________
- ______________________________________________

### Recommendations

- ______________________________________________
- ______________________________________________

---

## Sign-off

**Tester Name**: _________________
**Date**: _________________
**Status**: PASS / FAIL / CONDITIONAL PASS
**Notes**:

____________________________________________________________
____________________________________________________________
____________________________________________________________

---

**Last Updated**: 2025-10-19
**Checklist Version**: 1.0
