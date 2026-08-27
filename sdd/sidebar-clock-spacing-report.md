# Sidebar Clock Spacing

## What was causing the gap

The clock section in `Pulsebar/Sidebar.xaml` renders as:

```
StackPanel (GroupPanel)
  StackPanel (MonitorTitle)   <- dot, icon, "Time" label
  Border (SectionDivider)     <- thin divider line
  StackPanel (MonitorPanel)   <- wraps the clock Label + optional date
    Label (ClockTime)         <- the time text itself
```

`ClockTime` (the Label showing the time) already had `Padding="0"` and `Margin="0"`, so it
was not contributing any space itself.

The actual space above the time text came from `MonitorPanel` in `Pulsebar/FluentStyle.xaml`,
which sets `Margin="0,6,0,0"` — a 6px top margin on the StackPanel that directly wraps the
clock Label. That 6px gap sits immediately between the divider line and the visible time text,
which is what reads as "the margin above the time".

(`SectionDivider` also carries its own `Margin="0,8,0,0"`, and `MonitorTitle` has
`Margin="0,0,0,8"` below the title row — those contribute to the space between the title row
and the divider line, but that space is shared identically by every section, not specific to
the clock, and is not the gap directly above the time text itself.)

`MonitorPanel` is a shared style — grepping `Pulsebar/Sidebar.xaml` shows it used in three
places: the clock (line 102) and twice more inside the per-monitor `DataTemplate` for CPU/RAM/GPU/
drive/network panels (lines 143 and 199). Zeroing the style itself would have removed the
top margin from every monitor row in the sidebar, not just the clock.

## Change made

Added a local `Margin="0"` directly on the clock's `MonitorPanel` StackPanel instance in
`Pulsebar/Sidebar.xaml` (the one wrapping the `ClockTime` Label and optional date TextBlock).
A local property value on an element always wins over a value set by a `Style` setter in WPF,
so this overrides the 6px top margin for the clock only, while every other section that uses
the shared `MonitorPanel` style (CPU, RAM, GPU, drives, network) keeps its normal `0,6,0,0`
margin, unchanged.

```xml
<StackPanel Style="{StaticResource MonitorPanel}" Margin="0">
    <Label Content="{Binding Path=Time, Mode=OneWay}" Style="{StaticResource ClockTime}" />
    ...
</StackPanel>
```

No shared style (`MonitorPanel`, `SectionDivider`, `ClockTime`, `MonitorTitle`, `GroupPanel`)
was edited, so nothing about other sections' layout changes.

## Verification

- `dotnet build Pulsebar/Pulsebar.csproj -c Release` — build succeeded, **0 errors**
  (141 pre-existing `CA1416` platform-compatibility warnings, unrelated to this change).
- Diff is a single-attribute addition (`Margin="0"`) on one `StackPanel` instance in
  `Pulsebar/Sidebar.xaml`; no other file touched.
- Could not visually launch `Pulsebar.exe` in this sandboxed session — the app's manifest
  requires administrator elevation (the known issue from the `fix-installer-elevation`
  branch/commit `9f71b33`), and this non-interactive shell cannot answer the UAC prompt
  (`Start-Process` returned "The operation was canceled by the user"; running the exe directly
  from bash returned "Permission denied"). The fix is scoped to a single local `Margin`
  override on one already-identified element, so the XAML-level reasoning above stands in for
  a screenshot; a manual run/visual check is recommended before merging if a UI review is
  wanted.

## Files changed

- `Pulsebar/Sidebar.xaml` — added `Margin="0"` to the clock's `MonitorPanel` StackPanel
  (~line 102).
