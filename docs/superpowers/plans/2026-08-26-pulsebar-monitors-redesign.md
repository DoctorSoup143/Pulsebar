# Pulsebar Monitors Tab Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Monitors tab's `DataGrid`+`RowDetailsTemplate` with collapsible cards per hardware category, each with Hardware/Metrics/Options sections — same data, same drag-drop, same every binding, new presentation.

**Architecture:** New styles in the existing `Pulsebar/SettingsStyle.xaml` (card chrome, a hand-built expand-toggle, metric chips), then a full rewrite of the Monitors `TabItem`'s content in `Pulsebar/Settings.xaml`. `Pulsebar/SettingsModel.cs` untouched.

**Tech Stack:** .NET 10, WPF, GongSolutions.Wpf.DragDrop (already referenced, `xmlns:dd`).

## Global Constraints

- `Pulsebar/SettingsModel.cs` is not modified in any task in this plan.
- Every binding path the current Monitors tab has must survive: `MonitorConfig.Enabled`, `MonitorConfig.Name` (read-only), drag-drop on the `MonitorConfig` collection; `HardwareConfig.Enabled`, `HardwareConfig.Name` (read-write), drag-drop on `HardwareOC`; `MetricConfig.Enabled`, `MetricConfig.Name` (read-only); `ConfigParam.Name`/`Value`/`TypeString`/`Tooltip`, with the existing `System.Boolean`/`System.Int32` `DataTrigger` templating and the existing `IntConverter` resource (already declared in `Settings.xaml`'s `Window.Resources`, do not redeclare it).
- `dotnet build Pulsebar.sln` must show 0 errors after every task.
- No native WPF `Expander` — use a hand-built `ToggleButton` + `Visibility`-triggered panel instead, per the spec's stated risk avoidance (`docs/superpowers/specs/2026-08-26-pulsebar-monitors-redesign.md`).
- Runtime verification requires a human-assisted launch (the sandboxed environment cannot launch this admin-manifest app) — build success plus a careful diff-level check of every preserved binding path is the available evidence until that happens.
- Color tokens (same as the rest of this redesign): background `#12141F`, surface/card `#1A1F2E`, border `#2A3040`, accent teal `#3FBBA4`, primary text `#E8EAF0`, secondary text `#B8BFCC`.

---

### Task 1: Add the card, expand-toggle, and metric-chip styles

**Files:**
- Modify: `Pulsebar/SettingsStyle.xaml`

**Interfaces:**
- Produces: `MonitorCard` (`TargetType="{x:Type Border}"`), `MonitorCardExpandToggle` (`TargetType="{x:Type ToggleButton}"`), `CardSectionLabel` (`TargetType="{x:Type TextBlock}"`), `MetricChip` (`TargetType="{x:Type ToggleButton}"`).

- [ ] **Step 1: Add the styles**

```xml
            <Style x:Key="MonitorCard" TargetType="{x:Type Border}">
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="BorderBrush" Value="#2A3040" />
                <Setter Property="BorderThickness" Value="1" />
                <Setter Property="CornerRadius" Value="8" />
                <Setter Property="Margin" Value="0,0,0,10" />
                <Setter Property="Padding" Value="0,0,0,4" />
            </Style>

            <Style x:Key="CardSectionLabel" TargetType="{x:Type TextBlock}">
                <Setter Property="Foreground" Value="#B8BFCC" />
                <Setter Property="FontSize" Value="11" />
                <Setter Property="FontWeight" Value="Bold" />
                <Setter Property="Margin" Value="16,14,16,8" />
            </Style>

            <Style x:Key="MonitorCardExpandToggle" TargetType="{x:Type ToggleButton}">
                <Setter Property="Cursor" Value="Hand" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ToggleButton}">
                            <Border Background="Transparent" Padding="16,12">
                                <DockPanel LastChildFill="True">
                                    <Path x:Name="Chevron" DockPanel.Dock="Right" Data="M 0 0 L 8 0 L 4 6 Z" Fill="#B8BFCC" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="10,0,0,0">
                                        <Path.RenderTransform>
                                            <RotateTransform Angle="0" CenterX="4" CenterY="3" />
                                        </Path.RenderTransform>
                                    </Path>
                                    <ContentPresenter VerticalAlignment="Center" />
                                </DockPanel>
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsChecked" Value="True">
                                    <Setter TargetName="Chevron" Property="RenderTransform">
                                        <Setter.Value>
                                            <RotateTransform Angle="180" CenterX="4" CenterY="3" />
                                        </Setter.Value>
                                    </Setter>
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>

            <Style x:Key="MetricChip" TargetType="{x:Type ToggleButton}">
                <Setter Property="Padding" Value="10,5" />
                <Setter Property="Margin" Value="0,0,6,6" />
                <Setter Property="Foreground" Value="#B8BFCC" />
                <Setter Property="Cursor" Value="Hand" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ToggleButton}">
                            <Border x:Name="Chip" CornerRadius="12" Background="#12141F" BorderBrush="#2A3040" BorderThickness="1" Padding="{TemplateBinding Padding}">
                                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" TextElement.Foreground="{TemplateBinding Foreground}" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsChecked" Value="True">
                                    <Setter TargetName="Chip" Property="Background" Value="#3FBBA4" />
                                    <Setter TargetName="Chip" Property="BorderBrush" Value="#3FBBA4" />
                                    <Setter Property="Foreground" Value="#0E1220" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
```

(`MonitorCardExpandToggle`'s chevron rotation uses the same "replace the whole `RenderTransform` property via `Setter`" pattern as the sidebar's `ToggleSwitch` — do not try to name and target the nested `RotateTransform` directly, that's the exact `MC4111` error already hit once this session. `MetricChip`'s `TextElement.Foreground="{TemplateBinding Foreground}"` cascading-to-generated-TextBlock trick is the same one already proven working in `SettingTabItem`.)

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml
git commit -m "Add Monitors card, expand-toggle, and metric-chip styles"
```

---

### Task 2: Rebuild the Monitors tab as collapsible cards

**Files:**
- Modify: `Pulsebar/Settings.xaml`

**Interfaces:**
- Consumes: `MonitorCard`, `MonitorCardExpandToggle`, `CardSectionLabel`, `MetricChip` (Task 1), `SettingsTextBox`, `ToggleSwitch`, `SettingsRow`, `SettingsLabel` (already existing from the Settings redesign plan).
- Bindings this task must preserve, checked one by one after writing: `MonitorConfig.Enabled`, `.Name`, drag-drop on the outer `ItemsControl`; `HardwareConfig.Enabled`, `.Name`, drag-drop on the inner `ItemsControl`; `MetricConfig.Enabled`, `.Name`; `ConfigParam.Name`/`Value`/`TypeString`/`Tooltip` with the `IntConverter` resource key (already declared in `Settings.xaml`, do not redeclare).

- [ ] **Step 1: Replace the Monitors `TabItem`'s content**

Find the `TabItem` with `Header="{x:Static frame:Resources.SettingsMonitorsTab}"`. Its current content (a `StackPanel` containing a `SettingTitle`-styled subtitle block, then a `DataGrid`) should be replaced entirely, keeping only the subtitle `StackPanel` at the top:

```xml
            <TabItem Header="{x:Static frame:Resources.SettingsMonitorsTab}">
                <StackPanel Style="{StaticResource VerticalPanel}">
                    <StackPanel Style="{StaticResource SettingTitle}">
                        <TextBlock Text="{x:Static frame:Resources.SettingsMonitorsSubtitle1}" />
                        <TextBlock Text="{x:Static frame:Resources.SettingsMonitorsSubtitle2}" />
                    </StackPanel>

                    <ItemsControl ItemsSource="{Binding Path=MonitorConfig, Mode=OneWay}" dd:DragDrop.IsDragSource="True" dd:DragDrop.IsDropTarget="True">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="{x:Type monitor:MonitorConfig}">
                                <Border Style="{StaticResource MonitorCard}">
                                    <StackPanel>
                                        <DockPanel>
                                            <CheckBox DockPanel.Dock="Right" Style="{StaticResource ToggleSwitch}" Margin="0,0,16,0" IsChecked="{Binding Path=Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsMonitorEnabledTooltip}" />
                                            <ToggleButton x:Name="CardExpand" Style="{StaticResource MonitorCardExpandToggle}">
                                                <DockPanel>
                                                    <Ellipse Style="{StaticResource SectionDot}" />
                                                    <TextBlock Text="{Binding Path=Name, Mode=OneWay}" Foreground="#E8EAF0" FontWeight="Bold" VerticalAlignment="Center" />
                                                </DockPanel>
                                            </ToggleButton>
                                        </DockPanel>

                                        <StackPanel Visibility="Collapsed">
                                            <StackPanel.Style>
                                                <Style TargetType="{x:Type StackPanel}">
                                                    <Style.Triggers>
                                                        <DataTrigger Binding="{Binding IsChecked, ElementName=CardExpand}" Value="True">
                                                            <Setter Property="Visibility" Value="Visible" />
                                                        </DataTrigger>
                                                    </Style.Triggers>
                                                </Style>
                                            </StackPanel.Style>

                                            <TextBlock Text="Hardware" Style="{StaticResource CardSectionLabel}" />
                                            <ItemsControl Margin="16,0" ItemsSource="{Binding Path=HardwareOC, Mode=OneWay}" dd:DragDrop.IsDragSource="True" dd:DragDrop.IsDropTarget="True">
                                                <ItemsControl.ItemTemplate>
                                                    <DataTemplate DataType="{x:Type monitor:HardwareConfig}">
                                                        <DockPanel Margin="0,0,0,8">
                                                            <CheckBox DockPanel.Dock="Left" Style="{StaticResource ToggleSwitch}" Margin="0,0,10,0" IsChecked="{Binding Path=Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsHardwareEnabledTooltip}" />
                                                            <TextBox Style="{StaticResource SettingsTextBox}" Text="{Binding Path=Name, Mode=TwoWay}" ToolTip="{x:Static frame:Resources.SettingsHardwareNameTooltip}" />
                                                        </DockPanel>
                                                    </DataTemplate>
                                                </ItemsControl.ItemTemplate>
                                            </ItemsControl>

                                            <TextBlock Text="Metrics" Style="{StaticResource CardSectionLabel}" />
                                            <ItemsControl Margin="16,0" ItemsSource="{Binding Path=Metrics, Mode=OneWay}">
                                                <ItemsControl.ItemsPanel>
                                                    <ItemsPanelTemplate>
                                                        <WrapPanel />
                                                    </ItemsPanelTemplate>
                                                </ItemsControl.ItemsPanel>
                                                <ItemsControl.ItemTemplate>
                                                    <DataTemplate DataType="{x:Type monitor:MetricConfig}">
                                                        <ToggleButton Style="{StaticResource MetricChip}" Content="{Binding Path=Name, Mode=OneWay}" IsChecked="{Binding Path=Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsMetricsTooltip}" />
                                                    </DataTemplate>
                                                </ItemsControl.ItemTemplate>
                                            </ItemsControl>

                                            <TextBlock Text="Options" Style="{StaticResource CardSectionLabel}" />
                                            <ItemsControl Margin="16,0,16,12" ItemsSource="{Binding Path=Params}">
                                                <ItemsControl.ItemTemplate>
                                                    <DataTemplate DataType="{x:Type monitor:ConfigParam}">
                                                        <ContentControl Margin="0,0,0,8">
                                                            <ContentControl.Style>
                                                                <Style TargetType="{x:Type ContentControl}">
                                                                    <Style.Triggers>
                                                                        <DataTrigger Binding="{Binding TypeString}" Value="System.Boolean">
                                                                            <Setter Property="Content">
                                                                                <Setter.Value>
                                                                                    <DockPanel>
                                                                                        <CheckBox DockPanel.Dock="Left" Style="{StaticResource ToggleSwitch}" Margin="0,0,10,0" IsChecked="{Binding Path=Value, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{Binding Path=Tooltip, Mode=OneTime}" />
                                                                                        <TextBlock Text="{Binding Path=Name}" Foreground="#E8EAF0" VerticalAlignment="Center" />
                                                                                    </DockPanel>
                                                                                </Setter.Value>
                                                                            </Setter>
                                                                        </DataTrigger>
                                                                        <DataTrigger Binding="{Binding TypeString}" Value="System.Int32">
                                                                            <Setter Property="Content">
                                                                                <Setter.Value>
                                                                                    <StackPanel>
                                                                                        <TextBlock Text="{Binding Path=Name, Mode=OneTime}" Foreground="#B8BFCC" Margin="0,0,0,4" />
                                                                                        <TextBox Style="{StaticResource SettingsTextBox}" Width="80" HorizontalAlignment="Left" Text="{Binding Path=Value, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, Converter={StaticResource IntConverter}}" ToolTip="{Binding Path=Tooltip, Mode=OneTime}" />
                                                                                    </StackPanel>
                                                                                </Setter.Value>
                                                                            </Setter>
                                                                        </DataTrigger>
                                                                    </Style.Triggers>
                                                                </Style>
                                                            </ContentControl.Style>
                                                        </ContentControl>
                                                    </DataTemplate>
                                                </ItemsControl.ItemTemplate>
                                            </ItemsControl>
                                        </StackPanel>
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </TabItem>
```

Notes on specific choices, so you understand why this looks the way it does rather than just transcribing blindly:

- The `CheckBox`/`Enabled` toggle is a **sibling** of the `ToggleButton x:Name="CardExpand"` in the header `DockPanel`, not nested inside it — clicking the enable-switch must not also flip the card's expand/collapse state, and nesting one clickable control inside another causes exactly that kind of event-bubbling conflict.
- The collapse/expand `StackPanel`'s `Visibility` is driven by a `DataTrigger` with `ElementName=CardExpand` — this works correctly per-card because each `ItemsControl` item gets its own `DataTemplate` instance with its own `NameScope`, so `CardExpand` inside one card's template never collides with another card's `CardExpand`.
- The `SectionDot` style referenced for the header's `Ellipse` already exists (added earlier in the sidebar reskin plan, in `FluentStyle.xaml`) — confirm it's still resolvable from `Settings.xaml`'s context (it should be, since `FluentStyle.xaml` and `SettingsStyle.xaml` are both merged into the same `App.xaml` `Application.Resources`, and `Settings.xaml` is a Window like `Sidebar.xaml`, which correctly resolves merged-dictionary resources — this is the "does work" `StaticResource` direction established earlier this session, not the broken dictionary-to-includer direction).
- The `Options` section's `System.Boolean`/`System.Int32` `DataTrigger`-on-`TypeString` structure, and the `IntConverter` reference, are carried over unchanged from the original — only the controls inside each branch were restyled (`CheckBox`→`ToggleSwitch`-styled, `Label`→plain styled `TextBlock`, `TextBox`→`SettingsTextBox`-styled). Do not alter the `TypeString` matching logic itself.

- [ ] **Step 2: Confirm every preserved binding, one by one**

After writing the replacement, re-read it and check off each of these against what you wrote (do not just assume — actually find the line):
- `MonitorConfig.Enabled` — TwoWay, PropertyChanged, on a `ToggleSwitch`-styled `CheckBox`.
- `MonitorConfig.Name` — OneWay, in the header `TextBlock`.
- Outer `ItemsControl` has both `dd:DragDrop.IsDragSource="True"` and `dd:DragDrop.IsDropTarget="True"`.
- `HardwareConfig.Enabled` — TwoWay, PropertyChanged.
- `HardwareConfig.Name` — TwoWay (no `UpdateSourceTrigger` override needed, matches original which also had none explicit here — check the original had `Mode=TwoWay` only, no `UpdateSourceTrigger=PropertyChanged`, and preserve that exactly).
- Inner `ItemsControl` (hardware) has both drag-drop attached properties.
- `MetricConfig.Enabled` — TwoWay, PropertyChanged.
- `MetricConfig.Name` — OneWay.
- `ConfigParam.Name`, `.Value` (TwoWay, PropertyChanged, with `IntConverter` on the int branch only), `.TypeString` (used only as the `DataTrigger` binding, not displayed), `.Tooltip` (OneTime, on both branches).

- [ ] **Step 3: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and verify**

Controller + human user step: launch the app, open Settings → Monitors. Confirm: cards render collapsed by default, clicking a card header expands it (chevron rotates) without toggling that card's own Enabled switch, the Enabled switch works independently, drag-reordering cards works, expanding a card shows Hardware/Metrics/Options sections, hardware enable/rename still works and is drag-reorderable, metric chips toggle on click, boolean/int options still read and write correctly.

- [ ] **Step 5: Commit**

```bash
git add Pulsebar/Settings.xaml
git commit -m "Rebuild the Monitors tab as collapsible cards"
```

---

### Task 3: Final verification and documentation update

**Files:**
- Modify: `docs/superpowers/specs/2026-08-26-pulsebar-monitors-redesign.md`
- Modify (Obsidian vault): `Pulsebar/Project Overview.md`

- [ ] **Step 1: Full clean build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 2: Full run-through**

Controller + human user step: full pass over the Monitors tab across all 5 monitor types (CPU, RAM, GPU, Drives, Network) — not just one, since each has different `Params` shapes and different `Metrics` counts, and the `WrapPanel` chip layout and card height should be checked against the type with the most metrics (likely GPU or CPU) to confirm nothing overflows awkwardly.

- [ ] **Step 3: Update the spec doc to reflect the finished state**

In `docs/superpowers/specs/2026-08-26-pulsebar-monitors-redesign.md`, update it to describe the shipped state rather than the pre-implementation design. Edit in place — current-state doc, not a changelog.

- [ ] **Step 4: Update the Obsidian project overview**

In `Pulsebar/Project Overview.md`: note the Monitors tab redesign is complete.

- [ ] **Step 5: Final commit**

```bash
git add docs/superpowers/specs/2026-08-26-pulsebar-monitors-redesign.md
git commit -m "Update Monitors redesign spec to reflect the shipped state"
```
