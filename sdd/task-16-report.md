# Task 16: Narrow sidebar 5%, match drive bar height to CPU/RAM

**Status:** DONE_WITH_CONCERNS  
**Commit:** 31adf7e (Task 16: Narrow sidebar 5%, match drive bar height to CPU/RAM)  
**Date:** 2026-08-27

## Implementation

Implemented both required changes:

1. **Sidebar width reduction (5%):** Changed default sidebar width from 260 to 247 pixels
   - File: `Pulsebar/Settings.cs` line 366
   - Changed: `private int _sidebarWidth { get; set; } = 260;` → `= 247;`
   - Calculation: 260 × 0.95 = 247

2. **Drive bar height unification:** Changed drive load bar height from 9 to 4 to match CPU/RAM/GPU bars
   - File: `Pulsebar/FluentStyle.xaml` line 211
   - Changed: `<Setter Property="Height" Value="9" />` → `<Setter Property="Height" Value="4" />`
   - Reference: MetricLoadBar style already uses Height="4"

## Build Output

```
Build FAILED.
    143 Warning(s)
    2 Error(s)

Time Elapsed 00:00:12.53
```

**Errors (both file-lock related):**
- MSB3021: Unable to copy apphost.exe to bin\Debug\net10.0-windows\Pulsebar.exe
- MSB3027: Could not copy apphost.exe (Exceeded retry count)
- Root cause: File locked by running Pulsebar.exe process (PID 10632)

**Warnings (all pre-existing platform-specific CA1416):**
- 143 warnings related to Windows-only APIs in Utilities.cs (not affected by this task)

**Compilation status:** 0 compilation errors. Build failure is purely due to file-lock on EXE output, not source code issues.

## Files Changed

- `Pulsebar/Settings.cs` - 1 line changed (sidebar width)
- `Pulsebar/FluentStyle.xaml` - 1 line changed (drive bar height)

**Total changes:** 2 files, 2 insertions, 2 deletions

## Self-Review Findings

✅ Both values calculated/verified correctly:
- Sidebar reduction: 260 × 0.95 = 247.0
- Bar height: Drive bar changed from 9 to 4 (matching MetricLoadBar height of 4)

✅ Changes are minimal and surgical:
- Only the specified values changed
- No other properties or styles modified
- No related files require updates

✅ Consistency verified:
- MetricLoadBar (CPU/RAM/GPU) uses Height="4" (line 337 of FluentStyle.xaml)
- DriveProgress (drive load bar) now uses Height="4" (line 211 of FluentStyle.xaml)
- Both bars now uniform in height

✅ No cascading changes needed:
- Settings model doesn't need updates (property already exists and is used)
- No XAML binding changes required
- No resource dictionaries affected

## Concerns

**Build file-lock error:** The `dotnet build` command failed with MSB3027/MSB3021 errors because a stray Pulsebar.exe process (PID 10632) is locking the output binary. This is the known environment issue described in the task pre-flight notes. The compilation succeeded (0 compile errors); only the final copy operation failed. This is not a code issue.

**Mitigation:** If the user needs to verify the binary was created, they would need to:
1. Terminate the running Pulsebar.exe process, OR
2. Run `dotnet build` again after the process exits

The source code changes are correct and ready for deployment.

## Verification Summary

- ✅ Both changes implemented exactly as specified
- ✅ No syntax errors or breaking changes
- ✅ Follows existing code patterns and conventions
- ✅ Committed with descriptive message
- ⚠️ Build failed due to known file-lock issue (not a code problem)
