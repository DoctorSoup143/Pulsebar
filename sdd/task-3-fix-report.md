# Task 3 Fix Report — FontToSpaceConverter Cross-Dictionary StaticResource Crash

## Root cause confirmation

Confirmed exactly as described. `SidebarDiagnostics/FluentStyle.xaml`'s `MetricLabel` style
(line 179) referenced `{StaticResource FontToSpaceConverter}` inside a `Setter`, but the
converter was declared only in `SidebarDiagnostics/App.xaml`'s `Application.Resources`
(the dictionary that merges `FluentStyle.xaml` in). WPF's `StaticResource` lookup from
within a merged dictionary's own XAML does not walk back up into the includer's resources,
so the lookup failed at parse time with:

```
System.Exception: Cannot find resource named 'FontToSpaceConverter'.
```

This fires as soon as the `MetricLabel` style template is applied (any metric row render),
crashing the app on launch.

## What I changed

1. **`SidebarDiagnostics/App.xaml`**: removed the line
   `<conv:FontToSpaceConverter x:Key="FontToSpaceConverter" />` from
   `Application.Resources` (it was the only declaration; grep confirmed no other
   file references `FontToSpaceConverter`, so nothing else in `App.xaml` or elsewhere
   needed it to stay there).
2. **`SidebarDiagnostics/FluentStyle.xaml`**:
   - Added `xmlns:conv="clr-namespace:SidebarDiagnostics.Converters"` to the root
     `<ResourceDictionary>` element (previously only had `style`, `frame`, `win`
     prefixes).
   - Added `<conv:FontToSpaceConverter x:Key="FontToSpaceConverter" />` as a top-level
     resource in `FluentStyle.xaml`, co-located just above the `MetricLabel`/`SidebarWindow`
     styles, immediately before the `Style x:Key="SidebarWindow"` (i.e. at the top of the
     dictionary), so it is now declared in the same dictionary as its only consumer.

This is a "move declaration into consumer's dictionary" fix — no duplication, single
declaration, same class as recommended in the task brief.

## Other cross-dictionary StaticResource issues found

Checked every `{StaticResource ...}` reference inside `FluentStyle.xaml` against what's
declared in `App.xaml`:

- `FontToSpaceConverter` — **broken, fixed** (above).
- All other `StaticResource` references in `FluentStyle.xaml` (`DataText`, `MetricLabel`,
  `IconButton`, `MinScrollBar`, etc. via `BasedOn`) resolve to styles declared within
  `FluentStyle.xaml` itself — no cross-dictionary issue.
- `MetricLabelConverter`, `BoolInverseConverter`, `PercentConverter` remain declared in
  `App.xaml`. Grepped all `.xaml` files for these three converter keys:
  - `MetricLabelConverter` — used in `Sidebar.xaml` (2 places). Not referenced anywhere
    inside `FluentStyle.xaml`.
  - `BoolInverseConverter` — used in `Sidebar.xaml` and `Settings.xaml`. Not referenced
    inside `FluentStyle.xaml`.
  - `PercentConverter` — used only inside `App.xaml`'s own `UpdateProgress` style
    (line 306). Not referenced inside `FluentStyle.xaml`.

  These three are consumed only by Window-level XAML files (`Sidebar.xaml`,
  `Settings.xaml`) or by `App.xaml` itself — Window-to-Application resource lookups
  work fine in WPF (they walk up the logical tree to `Application.Resources`), unlike
  the merged-dictionary-to-includer lookup that broke `FontToSpaceConverter`. No action
  needed; left as-is.
- Also checked `FlatStyle.xaml` (the other dictionary merged in alongside `FluentStyle.xaml`)
  for the same bug class — all of its `StaticResource` references resolve to styles
  declared within `FlatStyle.xaml` itself. No issue found there.

**Conclusion: `FontToSpaceConverter` was the only instance of this bug class.** No
design-decision escalation needed.

## Build output (last ~10 lines)

```
    141 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.72
```

All 141 warnings are pre-existing `CA1416` platform-compatibility warnings (Windows-only
APIs like `PerformanceCounter`, `TaskService`, `WindowBackdrop`) unrelated to this change.

## Self-review findings

- Verified via grep that `FontToSpaceConverter` had exactly one declaration (now in
  `FluentStyle.xaml`) and exactly one consumer (`MetricLabel` Setter, same file) —
  no duplication, no dangling reference.
- Verified `App.xaml` still declares and uses `PercentConverter`, `MetricLabelConverter`,
  `BoolInverseConverter` correctly (unchanged).
- Verified the new `xmlns:conv` prefix in `FluentStyle.xaml` doesn't collide with any
  existing prefix (`style`, `frame`, `win` were the only ones present).
- Build succeeded with 0 errors after the change.

## Concerns

- None. Per the task instructions I did not attempt to launch the app (known sandbox
  limitation — requires admin elevation). The controller should ask the user to relaunch
  and confirm the `FontToSpaceConverter` XamlParseException no longer occurs.
