# Pulsebar Settings Window Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `Pulsebar/Settings.xaml` with a regrouped 6-tab layout (General/Appearance/Display/Advanced/Monitors/Hotkeys) and a set of custom-styled controls (toggle switch, dropdown, text field, slider) matching the sidebar's navy/teal identity, with zero behavior change.

**Architecture:** New dedicated resource dictionary `Pulsebar/SettingsStyle.xaml` (mirroring the existing `FluentStyle.xaml` pattern) holds every new/updated Settings-specific style. `Settings.xaml` itself is rewritten tab-by-tab to use the new styles and new tab grouping. `Pulsebar/SettingsModel.cs` is never touched — every binding path that exists today must exist, unchanged, in the rebuilt XAML.

**Tech Stack:** .NET 10, WPF. No new packages.

## Global Constraints

- `Pulsebar/SettingsModel.cs` must not be modified in any task in this plan. This is a view-only rebuild.
- Every `{Binding Path=X}` present in the current `Settings.xaml` must still exist, bound to the same `X`, somewhere in the rebuilt file — reorganizing where a control appears must never change what it's bound to. Tasks that touch a tab must list every binding path that tab currently has and confirm each survives.
- `dotnet build Pulsebar.sln` must show 0 errors after every task. There is no unit test project in this repo — build success plus the runtime verification method below are the available evidence.
- **Runtime verification requires a human-assisted launch.** The sandboxed environment cannot launch this admin-manifest app itself (established earlier in this session). For every task that changes what Settings.xaml renders, the verification step is: build clean, then hand off to the controller, who asks the human user to launch the app, open Settings, and confirm both appearance AND that each changed control still reads/writes its setting correctly (toggle it, close and reopen Settings, confirm the value persisted) — a visual-only check is not sufficient for this plan, since the real risk is broken bindings, not just wrong colors.
- No new localized resource strings. Section grouping within tabs is done with visual dividers/spacing (reusing the sidebar's `SectionDivider` pattern), not new labeled sub-headers — adding new label text would mean adding a new key across all 14 `Resources*.resx` files, which is out of proportion to what this redesign needs. This was not in the original spec's exact wording but is a necessary scope boundary discovered while planning; flagged here rather than silently applied.
- Color tokens (reuse across every new style in this plan): background `#12141F`, surface/row `#1A1F2E`, border `#2A3040`, accent teal `#3FBBA4`, accent teal hover/pressed `#2FA08C`, primary text `#E8EAF0`, secondary text `#B8BFCC`.

---

## File structure

- **Create:** `Pulsebar/SettingsStyle.xaml`, `Pulsebar/SettingsStyle.xaml.cs` — new dictionary, mirrors `FluentStyle.xaml`'s skeleton exactly. Holds: `ToggleSwitch`, `SettingsComboBox`, `SettingsComboBoxItem`, `SettingsTextBox`, `SettingsSlider`, `SettingsRow`, `SettingsSectionDivider`, `SettingsLabel`.
- **Modify:** `Pulsebar/App.xaml` — merge in `SettingsStyle.xaml` (after `FluentStyle.xaml`).
- **Modify:** `Pulsebar/Settings.xaml` — full rewrite of the `<TabControl>` body, tab by tab, across Tasks 6-10. Window chrome (`Style="{StaticResource FlatWindowStyle}"`, `Width="560"`) is untouched — already dark-themed and widened by Task 15.

---

### Task 1: Create the SettingsStyle resource dictionary skeleton

**Files:**
- Create: `Pulsebar/SettingsStyle.xaml`
- Create: `Pulsebar/SettingsStyle.xaml.cs`
- Modify: `Pulsebar/App.xaml`

**Interfaces:**
- Produces: an empty, merged `SettingsStyle.xaml` dictionary that Tasks 2-5 add styles to.

- [ ] **Step 1: Create `SettingsStyle.xaml`**

```xml
<ResourceDictionary
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:style="clr-namespace:Pulsebar.Style"
        x:Class="Pulsebar.Style.SettingsStyle"
        x:ClassModifier="public">

</ResourceDictionary>
```

- [ ] **Step 2: Create `SettingsStyle.xaml.cs`**

```csharp
using System.Windows;

namespace Pulsebar.Style
{
	public partial class SettingsStyle : ResourceDictionary
	{
		public SettingsStyle()
		{
			InitializeComponent();
		}
	}
}
```

- [ ] **Step 3: Merge it into `App.xaml`**

In `Pulsebar/App.xaml`, find:

```xml
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="FlatStyle.xaml" />
                <ResourceDictionary Source="FluentStyle.xaml" />
            </ResourceDictionary.MergedDictionaries>
```

Change to:

```xml
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="FlatStyle.xaml" />
                <ResourceDictionary Source="FluentStyle.xaml" />
                <ResourceDictionary Source="SettingsStyle.xaml" />
            </ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 4: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`. No visual change yet.

- [ ] **Step 5: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml Pulsebar/SettingsStyle.xaml.cs Pulsebar/App.xaml
git commit -m "Add empty SettingsStyle resource dictionary, merged into App.xaml"
```

---

### Task 2: Build the ToggleSwitch control template

**Files:**
- Modify: `Pulsebar/SettingsStyle.xaml`

**Interfaces:**
- Produces: `ToggleSwitch` style, `TargetType="{x:Type CheckBox}"` — a drop-in replacement wherever the current file binds `IsChecked="{Binding Path=X, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"` on a settings-row `CheckBox`. Same `IsChecked` property, same binding — only the visual template changes, so every existing `CheckBox` binding in Settings.xaml keeps working unmodified when its `Style` attribute is added.

- [ ] **Step 1: Add the style**

```xml
            <Style x:Key="ToggleSwitch" TargetType="{x:Type CheckBox}">
                <Setter Property="Cursor" Value="Hand" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type CheckBox}">
                            <Border x:Name="Track" Width="40" Height="22" CornerRadius="11" Background="#2A3040" BorderBrush="#3A4356" BorderThickness="1" HorizontalAlignment="Left">
                                <Ellipse x:Name="Thumb" Width="16" Height="16" Fill="#B8BFCC" HorizontalAlignment="Left" Margin="2,0,0,0">
                                    <Ellipse.RenderTransform>
                                        <TranslateTransform x:Name="ThumbTransform" X="0" />
                                    </Ellipse.RenderTransform>
                                </Ellipse>
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsChecked" Value="True">
                                    <Setter TargetName="Track" Property="Background" Value="#3FBBA4" />
                                    <Setter TargetName="Track" Property="BorderBrush" Value="#3FBBA4" />
                                    <Setter TargetName="Thumb" Property="Fill" Value="#0E1220" />
                                    <Setter TargetName="ThumbTransform" Property="X" Value="18" />
                                </Trigger>
                                <Trigger Property="IsEnabled" Value="False">
                                    <Setter TargetName="Track" Property="Opacity" Value="0.4" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
```

(No `VisualStateManager`/animation — this codebase's existing custom templates, e.g. `IconButton`, `DriveProgress`, use plain property triggers, so this stays consistent. `IsChecked` on `CheckBox` is a `bool?` — this template only handles `True`/otherwise-false visually, which matches every current usage in Settings.xaml: none of them are three-state.)

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml
git commit -m "Add ToggleSwitch control template"
```

---

### Task 3: Build the SettingsComboBox control template

**Files:**
- Modify: `Pulsebar/SettingsStyle.xaml`

**Interfaces:**
- Produces: `SettingsComboBox` style (`TargetType="{x:Type ComboBox}"`) and `SettingsComboBoxItem` style (`TargetType="{x:Type ComboBoxItem}"`, referenced from inside the ComboBox style's `Style.Resources` so every item in the dropdown picks it up automatically without each `ComboBox` in `Settings.xaml` needing an explicit `ItemContainerStyle`).

This is the highest-risk template in this plan — WPF's `ComboBox` requires specific named template parts (a `Popup`, and a `ToggleButton` bound to `IsDropDownOpen`) to function at all; getting this wrong produces a `ComboBox` that either doesn't open or throws at runtime, not at build time (the exact failure class already hit once this session — build succeeds, only a real launch reveals it). Follow this template exactly; do not simplify it.

- [ ] **Step 1: Add the styles**

```xml
            <Style x:Key="SettingsComboBoxItem" TargetType="{x:Type ComboBoxItem}">
                <Setter Property="Padding" Value="10,6" />
                <Setter Property="Foreground" Value="#E8EAF0" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ComboBoxItem}">
                            <Border x:Name="Bg" Padding="{TemplateBinding Padding}" Background="Transparent">
                                <ContentPresenter />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsHighlighted" Value="True">
                                    <Setter TargetName="Bg" Property="Background" Value="#3FBBA4" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>

            <Style x:Key="SettingsComboBox" TargetType="{x:Type ComboBox}">
                <Setter Property="Foreground" Value="#E8EAF0" />
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="BorderBrush" Value="#2A3040" />
                <Setter Property="BorderThickness" Value="1" />
                <Setter Property="Padding" Value="10,6" />
                <Setter Property="ItemContainerStyle" Value="{StaticResource SettingsComboBoxItem}" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ComboBox}">
                            <Grid>
                                <ToggleButton x:Name="ToggleButton" Focusable="False" IsChecked="{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                                    <ToggleButton.Template>
                                        <ControlTemplate TargetType="{x:Type ToggleButton}">
                                            <Border CornerRadius="6" Background="{Binding Background, RelativeSource={RelativeSource TemplatedParent, AncestorType={x:Type ComboBox}}}" BorderBrush="{Binding BorderBrush, RelativeSource={RelativeSource TemplatedParent, AncestorType={x:Type ComboBox}}}" BorderThickness="{Binding BorderThickness, RelativeSource={RelativeSource TemplatedParent, AncestorType={x:Type ComboBox}}}">
                                                <Path Data="M 0 0 L 8 0 L 4 5 Z" Fill="#B8BFCC" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,12,0" />
                                            </Border>
                                        </ControlTemplate>
                                    </ToggleButton.Template>
                                </ToggleButton>
                                <ContentPresenter x:Name="ContentSite" IsHitTestVisible="False" Content="{TemplateBinding SelectionBoxItem}" ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}" ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}" Margin="{TemplateBinding Padding}" VerticalAlignment="Center" HorizontalAlignment="Left" />
                                <TextBox x:Name="PART_EditableTextBox" Visibility="Hidden" IsReadOnly="{TemplateBinding IsReadOnly}" />
                                <Popup x:Name="Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False" PopupAnimation="None">
                                    <Grid x:Name="DropDown" SnapsToDevicePixels="True" MinWidth="{TemplateBinding ActualWidth}" MaxHeight="{TemplateBinding MaxDropDownHeight}">
                                        <Border x:Name="DropDownBorder" Background="#1A1F2E" BorderBrush="#2A3040" BorderThickness="1" CornerRadius="6" Margin="0,2,0,0" />
                                        <ScrollViewer Margin="4" SnapsToDevicePixels="True">
                                            <StackPanel IsItemsHost="True" KeyboardNavigation.DirectionalNavigation="Contained" />
                                        </ScrollViewer>
                                    </Grid>
                                </Popup>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
```

(`PART_EditableTextBox` is included hidden, matching the standard WPF `ComboBox` re-template pattern — `IsEditable` is `False` by default on every `ComboBox` in this file, so it stays invisible and inert, but its presence avoids `ComboBox` internals looking for a template part that doesn't exist. The dropdown arrow is a plain `Path` triangle rather than a `Border`+glyph-font icon, keeping it dependency-free.)

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml
git commit -m "Add SettingsComboBox control template"
```

---

### Task 4: Build the SettingsTextBox and SettingsSlider control templates

**Files:**
- Modify: `Pulsebar/SettingsStyle.xaml`

**Interfaces:**
- Produces: `SettingsTextBox` (`TargetType="{x:Type TextBox}"`), `SettingsSlider` (`TargetType="{x:Type Slider}"`).

- [ ] **Step 1: Add the styles**

```xml
            <Style x:Key="SettingsTextBox" TargetType="{x:Type TextBox}">
                <Setter Property="Foreground" Value="#E8EAF0" />
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="BorderBrush" Value="#2A3040" />
                <Setter Property="BorderThickness" Value="1" />
                <Setter Property="Padding" Value="8,5" />
                <Setter Property="CaretBrush" Value="#3FBBA4" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type TextBox}">
                            <Border x:Name="Border" CornerRadius="5" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                                <ScrollViewer x:Name="PART_ContentHost" Margin="{TemplateBinding Padding}" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsKeyboardFocused" Value="True">
                                    <Setter TargetName="Border" Property="BorderBrush" Value="#3FBBA4" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>

            <Style x:Key="SettingsSlider" TargetType="{x:Type Slider}">
                <Setter Property="Height" Value="20" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type Slider}">
                            <Grid VerticalAlignment="Center">
                                <Border Height="4" CornerRadius="2" Background="#2A3040" />
                                <Track x:Name="PART_Track">
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Command="Slider.DecreaseLarge">
                                            <RepeatButton.Template>
                                                <ControlTemplate TargetType="{x:Type RepeatButton}">
                                                    <Border Height="4" CornerRadius="2" Background="#3FBBA4" />
                                                </ControlTemplate>
                                            </RepeatButton.Template>
                                        </RepeatButton>
                                    </Track.DecreaseRepeatButton>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Command="Slider.IncreaseLarge">
                                            <RepeatButton.Template>
                                                <ControlTemplate TargetType="{x:Type RepeatButton}">
                                                    <Border Background="Transparent" />
                                                </ControlTemplate>
                                            </RepeatButton.Template>
                                        </RepeatButton>
                                    </Track.IncreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Width="14" Height="14">
                                            <Thumb.Template>
                                                <ControlTemplate TargetType="{x:Type Thumb}">
                                                    <Ellipse Fill="#3FBBA4" Stroke="#0E1220" StrokeThickness="1" />
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
```

(`PART_Track` is the one required named part for `Slider` — `Track`'s own `DecreaseRepeatButton`/`Thumb`/`IncreaseRepeatButton` handle drag and click-to-jump automatically once that part is present and named correctly; this is the standard minimal `Slider` re-template shape.)

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml
git commit -m "Add SettingsTextBox and SettingsSlider control templates"
```

---

### Task 5: Build the row/section/label layout styles

**Files:**
- Modify: `Pulsebar/SettingsStyle.xaml`

**Interfaces:**
- Produces: `SettingsRow` (`TargetType="{x:Type DockPanel}"` — one label-left, control-right settings row), `SettingsSectionDivider` (`TargetType="{x:Type Border}"` — thin separator between clusters of rows within a tab), `SettingsLabel` (`TargetType="{x:Type Label}"` — the row caption), `SettingsTabPage` (`TargetType="{x:Type StackPanel}"` — the scrollable vertical container each tab's content sits in, replacing the old `Grid`+`RowDefinitions` layout).

This task moves the layout model from the old fixed-row `Grid` (every row a `38px` `RowDefinition`, brittle to add to) to a `StackPanel` of `SettingsRow` `DockPanel`s, each sizing to its own content — this is what makes the new tab regrouping (Task 6-9) practical without hand-counting grid rows.

- [ ] **Step 1: Add the styles**

```xml
            <Style x:Key="SettingsTabPage" TargetType="{x:Type StackPanel}">
                <Setter Property="Orientation" Value="Vertical" />
                <Setter Property="Margin" Value="20,16" />
            </Style>

            <Style x:Key="SettingsRow" TargetType="{x:Type DockPanel}">
                <Setter Property="LastChildFill" Value="True" />
                <Setter Property="Margin" Value="0,0,0,14" />
            </Style>

            <Style x:Key="SettingsSectionDivider" TargetType="{x:Type Border}">
                <Setter Property="Height" Value="1" />
                <Setter Property="Background" Value="#2A3040" />
                <Setter Property="Margin" Value="0,4,0,18" />
            </Style>

            <Style x:Key="SettingsLabel" TargetType="{x:Type Label}">
                <Setter Property="DockPanel.Dock" Value="Left" />
                <Setter Property="Foreground" Value="#E8EAF0" />
                <Setter Property="Padding" Value="0" />
                <Setter Property="VerticalAlignment" Value="Center" />
                <Setter Property="MinWidth" Value="170" />
            </Style>
```

(`SettingsLabel`'s `MinWidth="170"` keeps every row's control column starting at the same x-position regardless of caption length, the same alignment purpose the old `Grid.ColumnDefinitions` served — `DockPanel.Dock="Left"` plus a fixed `MinWidth` reproduces that without a `Grid`.)

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml
git commit -m "Add SettingsRow/SettingsSectionDivider/SettingsLabel/SettingsTabPage layout styles"
```

---

### Task 6: Rebuild the General tab

**Files:**
- Modify: `Pulsebar/Settings.xaml`

**Interfaces:**
- Consumes: `SettingsTabPage`, `SettingsRow`, `SettingsLabel`, `SettingsSectionDivider`, `SettingsComboBox`, `ToggleSwitch` (all from Task 1-5).
- Bindings this tab must preserve (verify each survives): `DockEdgeItems`/`DockEdge`, `ScreenItems`/`ScreenIndex`, `CultureItems`/`Culture`, `UseAppBar`, `AlwaysTop`, `AutoUpdate`, `RunAtStartup`, and `ShowTrayIcon` (moving here from the old Advanced tab, per the spec's regroup table) with its existing `x:Name="ShowTrayIconCheckbox"` and `Unchecked="ShowTrayIconCheckbox_Unchecked"` handler — that code-behind handler must stay wired, only the visual template changes.

- [ ] **Step 1: Replace the General `TabItem`'s content**

Find the first `TabItem` (`Header="{x:Static frame:Resources.SettingsGeneralTab}"`) and replace its entire `<Grid>...</Grid>` content with:

```xml
                <StackPanel Style="{StaticResource SettingsTabPage}">
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsDock}" />
                        <ComboBox Style="{StaticResource SettingsComboBox}" ItemsSource="{Binding Path=DockEdgeItems, Mode=OneWay}" DisplayMemberPath="Text" SelectedValuePath="Value" SelectedValue="{Binding Path=DockEdge, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsDockTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsScreen}" />
                        <ComboBox Style="{StaticResource SettingsComboBox}" ItemsSource="{Binding Path=ScreenItems, Mode=OneWay}" DisplayMemberPath="Text" SelectedValuePath="Index" SelectedValue="{Binding Path=ScreenIndex, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsScreenTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsLanguage}" />
                        <ComboBox Style="{StaticResource SettingsComboBox}" ItemsSource="{Binding Path=CultureItems, Mode=OneWay}" DisplayMemberPath="Text" SelectedValuePath="Value" SelectedValue="{Binding Path=Culture, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsLanguageTooltip}" />
                    </DockPanel>

                    <Border Style="{StaticResource SettingsSectionDivider}" />

                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsReserveSpace}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=UseAppBar, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsReserveSpaceTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsAlwaysOnTop}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=AlwaysTop, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsAlwaysOnTopTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsAutoUpdate}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=AutoUpdate, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsAutoUpdateTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsRunAtStartup}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=RunAtStartup, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsRunAtStartupTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsShowTrayIcon}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" x:Name="ShowTrayIconCheckbox" Unchecked="ShowTrayIconCheckbox_Unchecked" IsChecked="{Binding Path=ShowTrayIcon, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsShowTrayIconTooltip}" />
                    </DockPanel>
                </StackPanel>
```

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Run and verify**

Controller + human user step: launch the app, open Settings, confirm the General tab renders with the new layout, and spot-check at least 2 controls (e.g. toggle "Always on top", close/reopen Settings, confirm it held; change Dock and confirm the sidebar actually redocks).

- [ ] **Step 4: Commit**

```bash
git add Pulsebar/Settings.xaml
git commit -m "Rebuild the General settings tab"
```

---

### Task 7: Rebuild the Appearance tab

**Files:**
- Modify: `Pulsebar/Settings.xaml`
- Modify: `Pulsebar/Properties/Resources.resx` and all 13 other `Resources*.resx` files (rename the `SettingsCustomizeTab` value from "Customize" to "Appearance")

**Interfaces:**
- Bindings this tab must preserve: `SidebarWidth` (via a named `Slider`+`TextBox` pair, same `ElementName` binding pattern as today), `AutoBGColor`, `BGColor` (via `xctk:ColorPicker`, `IsEnabled` bound inverse of `AutoBGColor`), `BGOpacity`, `TextAlignItems`/`TextAlign`, `FontSettingItems`/`FontSetting`, `FontColor`, `AlertFontColor`, `AlertBlink`. Also preserve the `PreviewTextInput="NumberBox_PreviewTextInput"` handler on every numeric `TextBox`, and `ValueChanged="OffsetSlider_ValueChanged"` is NOT used on this tab's sliders (that's Advanced tab only — do not add it here).

- [ ] **Step 1: Rename the tab's displayed text from "Customize" to "Appearance"**

The old third `TabItem` has `Header="{x:Static frame:Resources.SettingsCustomizeTab}"`. Its *key* (`SettingsCustomizeTab`) can stay the same — renaming a resx key everywhere it's referenced is extra churn for no reader-visible benefit — but its *value* (the actual displayed text) must change to reflect that this tab is now "Appearance," not "Customize," or the new IA's tab names (General/Appearance/Display/Advanced/Monitors/Hotkeys) won't match what's on screen. In `Pulsebar/Properties/Resources.resx`, find the `<data name="SettingsCustomizeTab" ...>` element and change its `<value>` from `Customize` to `Appearance`. Do the same in every other `Resources*.resx` file, using each language's word for "Appearance" if you can, or the English word "Appearance" if not — same reasoning as Task 8's new key: every file needs *some* correct value, a missing or stale one is a worse outcome than an imperfect translation. (This key is a resx *value* edit, not a new key — much smaller than Task 8's new-key situation, and does not need the `Header=` binding itself touched, since it already correctly points at this key.)

Then replace the tab's `<Grid>...</Grid>` content with:

```xml
                <StackPanel Style="{StaticResource SettingsTabPage}">
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsSidebarWidth}" />
                        <DockPanel>
                            <TextBox DockPanel.Dock="Right" Width="50" Margin="10,0,0,0" Style="{StaticResource SettingsTextBox}" Text="{Binding ElementName=SidebarWidthSlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" PreviewTextInput="NumberBox_PreviewTextInput" />
                            <Slider x:Name="SidebarWidthSlider" Style="{StaticResource SettingsSlider}" Value="{Binding Path=SidebarWidth, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Minimum="100" Maximum="300" TickFrequency="5" LargeChange="100" ToolTip="{x:Static frame:Resources.SettingsSidebarWidthTooltip}" />
                        </DockPanel>
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsAutoBackground}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=AutoBGColor, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsAutoBackgroundTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsBackgroundColor}" />
                        <xctk:ColorPicker IsEnabled="{Binding Path=AutoBGColor, Mode=OneWay, Converter={StaticResource BoolInverseConverter}}" SelectedColor="{Binding Path=BGColor, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsBackgroundColorTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsBackgroundOpacity}" />
                        <DockPanel>
                            <TextBox DockPanel.Dock="Right" Width="50" Margin="10,0,0,0" Style="{StaticResource SettingsTextBox}" Text="{Binding ElementName=BGOpacitySlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" PreviewTextInput="NumberBox_PreviewTextInput" />
                            <Slider x:Name="BGOpacitySlider" Style="{StaticResource SettingsSlider}" Value="{Binding Path=BGOpacity, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Minimum="0.01" Maximum="1.0" LargeChange="0.1" TickFrequency="0.01" ToolTip="{x:Static frame:Resources.SettingsBackgroundOpacityTooltip}" />
                        </DockPanel>
                    </DockPanel>

                    <Border Style="{StaticResource SettingsSectionDivider}" />

                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsTextAlign}" />
                        <ComboBox Style="{StaticResource SettingsComboBox}" ItemsSource="{Binding Path=TextAlignItems, Mode=OneWay}" DisplayMemberPath="Text" SelectedValuePath="Value" SelectedValue="{Binding Path=TextAlign, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsTextAlignTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsFontSize}" />
                        <ComboBox Style="{StaticResource SettingsComboBox}" ItemsSource="{Binding Path=FontSettingItems, Mode=OneWay}" DisplayMemberPath="FontSize" SelectedValue="{Binding Path=FontSetting, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsFontSizeTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsFontColor}" />
                        <xctk:ColorPicker SelectedColor="{Binding Path=FontColor, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsFontColorTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsAlertFontColor}" />
                        <xctk:ColorPicker SelectedColor="{Binding Path=AlertFontColor, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsAlertFontColorTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsAlertBlink}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=AlertBlink, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsAlertBlinkTooltip}" />
                    </DockPanel>
                </StackPanel>
```

(`xctk:ColorPicker` keeps its existing `Margin="0,6"`/`UsingAlphaChannel`/etc. defaults from the still-present `SettingGrid` style's `Style.Resources` — wait: this tab no longer uses `Style="{StaticResource SettingGrid}"` on its container, so those implicit `ColorPicker` defaults from `App.xaml`'s `SettingGrid.Style.Resources` no longer apply here. Add `Margin="0,6"` explicitly on both `ColorPicker` elements above if you want the same vertical spacing as before — check visually during Step 3 and add it if the pickers look cramped against neighboring rows; this is a judgment call left to the implementer since it's a minor cosmetic detail, not a binding-correctness one.)

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Run and verify**

Controller + human user step: open Settings → Appearance, spot-check the sidebar width slider (drag it, confirm the live sidebar panel resizes), the background color picker (change it, confirm the sidebar tints), and the auto-background checkbox (confirm it disables the color picker when checked, matching today's behavior).

- [ ] **Step 4: Commit**

```bash
git add Pulsebar/Settings.xaml Pulsebar/Properties/Resources.resx Pulsebar/Properties/Resources.ar.resx Pulsebar/Properties/Resources.da.resx Pulsebar/Properties/Resources.de.resx Pulsebar/Properties/Resources.de-CH.resx Pulsebar/Properties/Resources.es.resx Pulsebar/Properties/Resources.fi.resx Pulsebar/Properties/Resources.fr.resx Pulsebar/Properties/Resources.it.resx Pulsebar/Properties/Resources.ja.resx Pulsebar/Properties/Resources.nl.resx Pulsebar/Properties/Resources.ru.resx Pulsebar/Properties/Resources.tr.resx Pulsebar/Properties/Resources.zh.resx
git commit -m "Rebuild the Appearance settings tab, rename its header from Customize"
```

---

### Task 8: Rebuild the Display tab

**Files:**
- Modify: `Pulsebar/Settings.xaml`

**Interfaces:**
- Bindings this tab must preserve: `ShowMachineName`, `ShowClock`, `Clock24HR` (with its existing `IsEnabled="{Binding Path=ShowClock, Mode=OneWay}"`), `DateSettingItems`/`DateSetting` (also `IsEnabled` bound to `ShowClock`).

- [ ] **Step 1: Move these four rows out of the old Customize tab into their own tab**

The current file has one `TabItem` per tab, in document order: General, Advanced, Customize (→ becomes Appearance, done in Task 7), Monitors, Hotkeys — there is no existing "Display" `TabItem` yet. Add a new `TabItem` immediately after the Appearance one (which Task 7 already renamed in content but not in position — Appearance's `TabItem` stays exactly where the old Customize one was in document order):

```xml
            <TabItem Header="{x:Static frame:Resources.SettingsDisplayTab}">
                <StackPanel Style="{StaticResource SettingsTabPage}">
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsShowMachineName}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=ShowMachineName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsShowMachineNameTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsShowClock}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=ShowClock, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsShowClockTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.Settings24HourClock}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=Clock24HR, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" IsEnabled="{Binding Path=ShowClock, Mode=OneWay}" ToolTip="{x:Static frame:Resources.Settings24HourClockTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsDateFormat}" />
                        <ComboBox Style="{StaticResource SettingsComboBox}" ItemsSource="{Binding Path=DateSettingItems}" DisplayMemberPath="Display" SelectedValue="{Binding Path=DateSetting, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" IsEnabled="{Binding Path=ShowClock, Mode=OneWay}" ToolTip="{x:Static frame:Resources.SettingsDateFormatTooltip}" />
                    </DockPanel>
                </StackPanel>
            </TabItem>
```

`{x:Static frame:Resources.SettingsDisplayTab}` does not exist yet in `Resources.resx` — the Global Constraints forbid adding new resx keys for new *body* text, but a new tab genuinely needs a new tab-header string; this is the one exception, since without it the tab would need a hardcoded (non-localized) English label, which is worse. Add a `SettingsDisplayTab` entry to `Pulsebar/Properties/Resources.resx` with value `Display`, and the same key with an appropriate translation to each of the other 13 `Resources*.resx` files (mirror how `SettingsCustomizeTab` is already translated in each — copy its structure, not its value). If translating into 13 languages isn't practical for this task, use the English word "Display" as the value in every file rather than leaving any file's entry missing — a missing resx key throws at runtime the same way a missing `StaticResource` does (same failure class already hit this session), so every file must have *some* value for this key even if not perfectly localized.

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Run and verify**

Controller + human user step: open Settings, confirm a "Display" tab now exists between Appearance and Monitors, confirm all 4 controls work, confirm toggling "Show clock" off actually disables (greys out) the 24hr-clock and date-format controls.

- [ ] **Step 4: Commit**

```bash
git add Pulsebar/Settings.xaml Pulsebar/Properties/Resources.resx Pulsebar/Properties/Resources.ar.resx Pulsebar/Properties/Resources.da.resx Pulsebar/Properties/Resources.de.resx Pulsebar/Properties/Resources.de-CH.resx Pulsebar/Properties/Resources.es.resx Pulsebar/Properties/Resources.fi.resx Pulsebar/Properties/Resources.fr.resx Pulsebar/Properties/Resources.it.resx Pulsebar/Properties/Resources.ja.resx Pulsebar/Properties/Resources.nl.resx Pulsebar/Properties/Resources.ru.resx Pulsebar/Properties/Resources.tr.resx Pulsebar/Properties/Resources.zh.resx
git commit -m "Add the Display settings tab"
```

---

### Task 9: Rebuild the Advanced tab

**Files:**
- Modify: `Pulsebar/Settings.xaml`

**Interfaces:**
- Bindings this tab must preserve: `UIScale`, `XOffset`/`YOffset` (both keep `ValueChanged="OffsetSlider_ValueChanged"` on their `Slider`s — this handler is specific to the offset sliders, do not add it to `UIScale`'s or `PollingInterval`'s sliders, matching today), `PollingInterval`, `ToolbarMode`, `ClickThrough` (keeps `x:Name="ClickThroughCheckbox"` and `Checked="ClickThroughCheckbox_Checked"`), `InitiallyHidden`, `CollapseMenuBar`.

- [ ] **Step 1: Replace the second `TabItem`'s (`SettingsAdvancedTab`) content**

Replace its `<Grid>...</Grid>` content with:

```xml
                <StackPanel Style="{StaticResource SettingsTabPage}">
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsUIScale}" />
                        <DockPanel>
                            <TextBox DockPanel.Dock="Right" Width="50" Margin="10,0,0,0" Style="{StaticResource SettingsTextBox}" Text="{Binding ElementName=UIScaleSlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" PreviewTextInput="NumberBox_PreviewTextInput" />
                            <Slider x:Name="UIScaleSlider" Style="{StaticResource SettingsSlider}" Value="{Binding Path=UIScale, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Minimum="0.5" Maximum="3.0" TickFrequency="0.1" LargeChange="0.5" ToolTip="{x:Static frame:Resources.SettingsUIScaleTooltip}" />
                        </DockPanel>
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsHorizontalOffset}" />
                        <DockPanel>
                            <TextBox DockPanel.Dock="Right" Width="50" Margin="10,0,0,0" Style="{StaticResource SettingsTextBox}" Text="{Binding ElementName=XOffsetSlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" PreviewTextInput="NumberBox_PreviewTextInput" />
                            <Slider x:Name="XOffsetSlider" Style="{StaticResource SettingsSlider}" Value="{Binding Path=XOffset, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Minimum="-2000" Maximum="2000" TickFrequency="1" LargeChange="1000" ValueChanged="OffsetSlider_ValueChanged" ToolTip="{x:Static frame:Resources.SettingsHorizontalOffsetTooltip}" />
                        </DockPanel>
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsVerticalOffset}" />
                        <DockPanel>
                            <TextBox DockPanel.Dock="Right" Width="50" Margin="10,0,0,0" Style="{StaticResource SettingsTextBox}" Text="{Binding ElementName=YOffsetSlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" PreviewTextInput="NumberBox_PreviewTextInput" />
                            <Slider x:Name="YOffsetSlider" Style="{StaticResource SettingsSlider}" Value="{Binding Path=YOffset, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Minimum="-2000" Maximum="2000" TickFrequency="1" LargeChange="1000" ValueChanged="OffsetSlider_ValueChanged" ToolTip="{x:Static frame:Resources.SettingsVerticalOffsetTooltip}" />
                        </DockPanel>
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsPollingInterval}" />
                        <DockPanel>
                            <TextBox DockPanel.Dock="Right" Width="50" Margin="10,0,0,0" Style="{StaticResource SettingsTextBox}" Text="{Binding ElementName=PollingIntervalSlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" PreviewTextInput="NumberBox_PreviewTextInput" />
                            <Slider x:Name="PollingIntervalSlider" Style="{StaticResource SettingsSlider}" Value="{Binding Path=PollingInterval, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Minimum="100" Maximum="5000" TickFrequency="100" LargeChange="1000" ToolTip="{x:Static frame:Resources.SettingsPollingIntervalTooltip}" />
                        </DockPanel>
                    </DockPanel>

                    <Border Style="{StaticResource SettingsSectionDivider}" />

                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsToolbarMode}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=ToolbarMode, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsToolbarModeTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsClickThrough}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" x:Name="ClickThroughCheckbox" Checked="ClickThroughCheckbox_Checked" IsChecked="{Binding Path=ClickThrough, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsClickThroughTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsInitiallyHidden}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=InitiallyHidden, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsInitiallyHiddenTooltip}" />
                    </DockPanel>
                    <DockPanel Style="{StaticResource SettingsRow}">
                        <Label Style="{StaticResource SettingsLabel}" Content="{x:Static frame:Resources.SettingsCollapseMenuBar}" />
                        <CheckBox Style="{StaticResource ToggleSwitch}" IsChecked="{Binding Path=CollapseMenuBar, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" ToolTip="{x:Static frame:Resources.SettingsCollapseMenuBarTooltip}" />
                    </DockPanel>
                </StackPanel>
```

- [ ] **Step 2: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 3: Run and verify**

Controller + human user step: open Settings → Advanced, spot-check UI scale (drag it, confirm the sidebar's text/icons resize) and one offset slider (drag it, confirm the sidebar visibly shifts position — that's what `OffsetSlider_ValueChanged` drives).

- [ ] **Step 4: Commit**

```bash
git add Pulsebar/Settings.xaml
git commit -m "Rebuild the Advanced settings tab"
```

---

### Task 10: Restyle the Monitors and Hotkeys tabs

**Files:**
- Modify: `Pulsebar/App.xaml` (`MonitorGrid`, `MonitorGridHeader`, `MonitorHardwareHeader`, `HotkeyLabel` — colors only, no structural change)
- Modify: `Pulsebar/FlatStyle.xaml` (`HotkeyToggle` — the "Bind" `ToggleButton` used in the Hotkeys tab; check whether it needs a color-only update or is already acceptable against the dark window from Task 15)

**Interfaces:** none new. Per the spec, the Monitors `DataGrid`/`ListView` and the Hotkeys `ToggleButton`/`TextBox` structure are explicitly NOT rebuilt from scratch in this plan — this task is colors/borders only.

- [ ] **Step 1: Darken the Monitors grid header and row-detail chrome**

In `Pulsebar/App.xaml`, find `MonitorGridHeader`:

```xml
            <Style x:Key="MonitorGridHeader" TargetType="{x:Type DataGridColumnHeader}">
                <Setter Property="Padding" Value="12,4" />
            </Style>
```

Add `Background`/`Foreground`:

```xml
            <Style x:Key="MonitorGridHeader" TargetType="{x:Type DataGridColumnHeader}">
                <Setter Property="Padding" Value="12,4" />
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="Foreground" Value="#E8EAF0" />
            </Style>
```

Find `MonitorHardwareHeader` and add the same two setters:

```xml
            <Style x:Key="MonitorHardwareHeader" TargetType="{x:Type GridViewColumnHeader}">
                <Setter Property="Padding" Value="12,4" />
                <Setter Property="IsHitTestVisible" Value="False" />
                <Setter Property="HorizontalContentAlignment" Value="Left" />
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="Foreground" Value="#E8EAF0" />
            </Style>
```

Find `MonitorGrid`'s `Style.Resources` (already has a `HighlightBrushKey` override from before this plan) and add row/cell background/foreground so the `DataGrid`'s body isn't left stark white against everything else now being dark:

```xml
                <Style.Resources>
                    <SolidColorBrush x:Key="{x:Static SystemColors.HighlightBrushKey}" Color="#E1E7E9" />
                    <Style TargetType="{x:Type DataGridCell}">
                        <Setter Property="BorderThickness" Value="0" />
                    </Style>
                </Style.Resources>
```

→

```xml
                <Style.Resources>
                    <SolidColorBrush x:Key="{x:Static SystemColors.HighlightBrushKey}" Color="#3FBBA4" />
                    <Style TargetType="{x:Type DataGridCell}">
                        <Setter Property="BorderThickness" Value="0" />
                        <Setter Property="Background" Value="#1A1F2E" />
                        <Setter Property="Foreground" Value="#E8EAF0" />
                    </Style>
                    <Style TargetType="{x:Type DataGridRow}">
                        <Setter Property="Background" Value="#1A1F2E" />
                    </Style>
                </Style.Resources>
```

(The `HighlightBrushKey` change, `#E1E7E9` → `#3FBBA4`, makes the selected-row color the app's teal accent instead of the old light-gray-blue — matches the sidebar's semantic/accent color use elsewhere, and was flagged as deliberately untouched in Task 15 specifically because the rows were still white *then*; now that this task darkens the rows too, updating the highlight to match is the correct completion of that, not a re-litigation of Task 15's choice.)

- [ ] **Step 2: Darken the hotkey display fields**

In `Pulsebar/App.xaml`, find `HotkeyLabel`:

```xml
            <Style x:Key="HotkeyLabel" TargetType="{x:Type TextBox}">
                <Setter Property="Height" Value="22" />
                <Setter Property="Padding" Value="5,0" />
                <Setter Property="VerticalContentAlignment" Value="Center" />
                <Setter Property="IsReadOnly" Value="True" />
            </Style>
```

Add colors:

```xml
            <Style x:Key="HotkeyLabel" TargetType="{x:Type TextBox}">
                <Setter Property="Height" Value="22" />
                <Setter Property="Padding" Value="5,0" />
                <Setter Property="VerticalContentAlignment" Value="Center" />
                <Setter Property="IsReadOnly" Value="True" />
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="Foreground" Value="#E8EAF0" />
                <Setter Property="BorderBrush" Value="#2A3040" />
            </Style>
```

In `Pulsebar/FlatStyle.xaml`, find `HotkeyToggle`:

```xml
    <Style x:Key="HotkeyToggle" TargetType="{x:Type ToggleButton}">
        <Setter Property="Content" Value="Bind" />
        <Setter Property="Height" Value="22" />
    </Style>
```

Add colors (this `ToggleButton` has no custom `ControlTemplate`, so it still renders with native chrome — same "restyle, don't rebuild" boundary as the rest of this task, just enough color to not look jarringly light):

```xml
    <Style x:Key="HotkeyToggle" TargetType="{x:Type ToggleButton}">
        <Setter Property="Content" Value="Bind" />
        <Setter Property="Height" Value="22" />
        <Setter Property="Background" Value="#1A1F2E" />
        <Setter Property="Foreground" Value="#E8EAF0" />
    </Style>
```

- [ ] **Step 3: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and verify**

Controller + human user step: open Settings → Monitors, confirm the grid/row-details area reads dark with teal row selection, expand a hardware row's details, confirm the nested hardware/metric checkboxes still work. Open Settings → Hotkeys, confirm the hotkey fields are dark, and confirm binding a new hotkey (click Bind, press a key combo) still works exactly as before — this is pure color styling, but the "Bind" interaction is stateful code-behind and worth confirming nothing broke.

- [ ] **Step 5: Commit**

```bash
git add Pulsebar/App.xaml Pulsebar/FlatStyle.xaml
git commit -m "Darken the Monitors grid and Hotkeys tab chrome"
```

---

### Task 11: Final verification pass and documentation update

**Files:**
- Modify: `docs/superpowers/specs/2026-08-26-pulsebar-settings-redesign.md`
- Modify (Obsidian vault): `Pulsebar/Project Overview.md`

- [ ] **Step 1: Full clean build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 2: Full run-through**

Controller + human user step: launch the app, open Settings, click through all 6 tabs (General, Appearance, Display, Advanced, Monitors, Hotkeys). For each tab, re-verify every binding listed in that tab's task (6-10) still round-trips correctly — this is the point of the plan where a systematic pass matters more than a quick glance, since 6 tasks' worth of rebinding risk has accumulated. Close Settings via both Save and Cancel/Close, confirm each behaves as it did before this plan (Save persists changes, Close/Cancel when `IsChanged` discards them — check `SettingsModel.cs`'s existing `IsChanged`-driven Save/Apply/Close logic if the exact discard behavior needs confirming, but do not modify that file).

- [ ] **Step 3: Update the spec doc to reflect the finished state**

In `docs/superpowers/specs/2026-08-26-pulsebar-settings-redesign.md`, update it to describe the *shipped* state (which tabs exist, what the control templates actually look like, any judgment calls made during implementation like Task 7's `ColorPicker` margin note) rather than the pre-implementation design. Edit in place — current-state doc, not a changelog, per this project's established documentation convention.

- [ ] **Step 4: Update the Obsidian project overview**

In `Pulsebar/Project Overview.md`: add a line noting the Settings window redesign is complete, and that Graph/Setup/Update/ChangeLog are the tracked follow-up phase (not yet started).

- [ ] **Step 5: Final commit**

```bash
git add docs/superpowers/specs/2026-08-26-pulsebar-settings-redesign.md
git commit -m "Update Settings redesign spec to reflect the shipped state"
```

---

### Task 12: Dark-theme the color picker swatches

**Why this task exists:** user feedback after reviewing the running app — the three `xctk:ColorPicker` controls (Appearance tab's BGColor/FontColor/AlertFontColor) still show Xceed's default light closed-state swatch button. Scoped explicitly to the closed-state control only, per the user's choice — the popup/canvas that opens when you click it stays native (a full custom re-template of that popup is a much larger, separate effort, similar in scale to the ComboBox work, and wasn't asked for).

**Files:**
- Modify: `Pulsebar/SettingsStyle.xaml` (new `SettingsColorPicker` style)
- Modify: `Pulsebar/Settings.xaml` (apply the new style to all 3 `ColorPicker` instances)

**Interfaces:**
- Produces: `SettingsColorPicker` style, `TargetType="{x:Type xctk:ColorPicker}"` (note the `xctk` namespace — this is the Xceed Extended WPF Toolkit control, already referenced via `xmlns:xctk` in both files).

This is deliberately a **Setters-only** style, no custom `ControlTemplate` — Xceed's `ColorPicker` is a third-party control whose internal template part names aren't documented the way WPF's built-in controls are, so writing a full re-template carries real risk of the same class of runtime failure already hit twice this session (ComboBox, and would-be Slider/TextBox issues avoided by staying simple). Setting `Background`/`BorderBrush`/`Foreground` works because Xceed's own default template does respect those standard properties via internal TemplateBinding — that's true of every well-built custom control in this toolkit (confirmed by the fact `UsingAlphaChannel`/`ColorMode`/`DisplayColorAndName`/`ShowStandardColors` already work as plain property setters elsewhere in this codebase, in `App.xaml`'s pre-existing `SettingGrid`-scoped `ColorPicker` style).

- [ ] **Step 1: Add the style**

In `Pulsebar/SettingsStyle.xaml`, add:

```xml
            <Style x:Key="SettingsColorPicker" TargetType="{x:Type xctk:ColorPicker}">
                <Setter Property="Background" Value="#1A1F2E" />
                <Setter Property="BorderBrush" Value="#2A3040" />
                <Setter Property="Foreground" Value="#E8EAF0" />
                <Setter Property="Padding" Value="6,4" />
            </Style>
```

This requires adding `xmlns:xctk="http://schemas.xceed.com/wpf/xaml/toolkit"` to `SettingsStyle.xaml`'s root `<ResourceDictionary>` element — it isn't there yet (this file has only needed `style`/`frame`/`win`/`conv` prefixes so far).

- [ ] **Step 2: Apply it to all three ColorPickers**

In `Pulsebar/Settings.xaml`, find each of the three `<xctk:ColorPicker ...>` elements (search for `xctk:ColorPicker` — one in the `BGColor` row, one in `FontColor`, one in `AlertFontColor`, all in the Appearance tab) and add `Style="{StaticResource SettingsColorPicker}"` to each, alongside their existing attributes (`Margin="0,6"`, `SelectedColor=...`, `ToolTip=...`, and for the `BGColor` one, `IsEnabled=...` too) — do not remove or reorder anything already there, just add the `Style` attribute.

- [ ] **Step 3: Build**

Run: `dotnet build Pulsebar.sln`
Expected: `0 Error(s)`.

- [ ] **Step 4: Run and verify**

Controller + human user step: launch the app, open Settings → Appearance, confirm all three color swatches now show a dark background/border instead of the native light chrome, and confirm clicking one still opens a working color-selection popup (native chrome there is expected and fine) and still changes the bound color correctly.

- [ ] **Step 5: Commit**

```bash
git add Pulsebar/SettingsStyle.xaml Pulsebar/Settings.xaml
git commit -m "Dark-theme the color picker swatches"
```
