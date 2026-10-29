# Components Overview

## Component Organization

This directory is for all shared components.

If a component is used solely for a single `Feature` then it should go in the `Components` Folder under the that Feature.
But if used by more than one Feature it should be in this folder.

## Component Hierarchy Naming Convention

### 🎯 Core Principle
Underscores in component names represent **hierarchical relationships** and are used consistently across both files and directories when those directories represent component containers.

### 📚 Organizational Tiers

#### **Tier 1: Simple Components**
**Pattern**: Single files  
**Location**: `Elements/`, `Forms/`, etc.  
**Examples**:
- `Card.razor`
- `StatusBadge.razor` — status pill (`twe-badge`); Active, Inactive, Warning, Info, and Error (the red lost/error status; the label carries the word); the caller can pass a label and extra attributes
- `ChartCard.razor` — chart frame (`twe-chart-card`); title, optional header, filter, plot slot, and legend; the page draws the plot
- `LineChart.razor` — plot for that slot (`twe-line-chart`); gridlines, one line per series, a dot on the last point; the page passes points, tones, and axis labels
- `ChartLegendItem.razor` — one legend row (`twe-chart-legend`); ink, primary, or positive dot
- `TrendPill.razor` — period move (`twe-trend`); up, down, or flat; the direction word is in the accessible name
- `SlaChip.razor` — on-time tier (`twe-sla-chip`); good, warning, or behind
- `ClassPill.razor` — classification pill (`twe-class-pill`); mapped ink wash, or dashed when unmapped
- `RoleBadge.razor` — role name on that pill; the page passes the word and the status
- `RoleCard.razor` — one selectable option (`twe-role-card`); the page owns selection, the roving tabindex (0 on the selected card, -1 on the others, through unmatched attributes), and the arrow keys (`RovingSelection`, `SegmentedControl`)
- `PermissionGrid.razor` — capability groups (`twe-perm-grid`); each row is granted or not
- `DevNote.razor` — callout (`twe-devnote`); optional eyebrow, optional title, and a body
- `Avatar.razor` — initials circle (`twe-avatar`); one or two letters; one brand fill; small, medium, and large
- `UserCard.razor` (composites) — identity row (`twe-user-card`); name, optional detail, optional link; the shell pins the signed-in person under the menu
- `EmptyState.razor` — empty list or panel (`twe-empty-state`); full paper card or inline; optional description, FluentUI icon, and actions
- `IntegrationCard.razor` — external system row (`twe-integration-card`); full identity banner or compact pull-status line; compact warning tone paints the warning surface; full is unchanged; optional FluentUI icon, meta, status badge, and actions
- `CountChip.razor` — number pill (`twe-count-chip`); neutral or accent; tabular numerals
- `GroupedTable.razor` — one table in a paper container (`twe-grouped-table`); the page supplies the header and the groups
- `twe-numeric` in `app.css`, beside `.twe-table` — right-aligned tabular numerals; the page puts it on `th` and `td`
- `DefinitionList.razor` / `DefinitionItem.razor` — wrapping grid of term and value (`twe-definition-list`); the value is any content; a blank term is omitted
- `PageSplit.razor` — primary and secondary columns (`twe-page-split`); 1.6fr and 1fr; stacks at 1100px, primary first
- `TableGroup.razor` — one row group (`twe-table-group`); title, optional count chip, optional meta; the page supplies the rows
- `Drawer.razor` — slide-over shell (`twe-drawer`); native modal dialog; end or start; medium or wide; optional footer
- `FormContainer.razor` (Card shell; `ActionContent` renders in `FormActions`)
- `FormSection.razor` / `FormGrid.razor` / `FormField.razor` / `FormActions.razor` — stacked form layout (full-width controls, `--twe-space-*` tokens)
- `TextArea.razor` (forms) — the only multi-line text field: fills its field, 9rem default height, vertical resize; raw `FluentTextArea` elsewhere is TWA0032
- `Section.razor` (composites) — Card + `FluentSpinner` while `ActionTrackingState.IsAnyActive(LoadingActionType)`
- `FilterBar.razor` (composites) — paper toolbar (`role="toolbar"`) with optional debounced search and filter `ChildContent`
- `FilterSelect.razor` (composites) — muted inline label and `FluentSelect`
- `Tabs.razor` (composites) — underline sub-view switcher (`role="tablist"`); items raise `ValueChanged`
- `SegmentedControl.razor` (composites) — pill group of mutually exclusive options (`role="radiogroup"`)
- `PeriodStepper.razor` (composites) — previous, a label, and next (`twe-period-stepper`); each end can be disabled; the page owns the label and `ValueChanged`
- `DataGridFrame.razor` (composites) — frame around every `FluentDataGrid`: scrolls sideways below the grid's minimum width, ellipsis headers; columns use `GridTemplateColumns` minmax tracks (tw-blazor)

**Status and outcomes:** operation outcomes (success/failure) go to the shell's single notification region (`MessageBars.razor` painting `NotificationState`) — never a page-local outcome bar (TWA0025). Static Info/Warning guidance may stay inline. Every bar is the shared `MessageBar.razor` with its Copy button, never a raw `FluentMessageBar` (TWA0037). Buttons: `FluentButton`.

#### **Tier 2: Complex Components** 
**Pattern**: Flat structure with underscore hierarchy  
**Examples**:
```
SitePage/
├── SitePage.razor
├── LeftPane_Header.razor       # Component parts as files
├── LeftPane_Main.razor
├── LeftPane_Footer.razor
├── RightPane_Header.razor
└── RightPane_Footer.razor
```

#### **Tier 3: Mega Components**
**Pattern**: Nested directories + underscore hierarchy  
**Examples**:
```
TimeWarpPage/
├── LeftPane/                   # Component part as directory
│   ├── LeftPane.razor
│   ├── LeftPane_Header.razor   # Sub-parts as files
│   ├── LeftPane_Main.razor
│   └── LeftPane_Footer.razor
└── RightPane/
    ├── RightPane.razor
    ├── RightPane_Header.razor
    ├── RightPane_Footer.razor
    └── RightPane_Main/         # Component part that contains others
        ├── AsidePane/          # Sub-component directory
        ├── PagePane/           # Sub-component directory
        └── RightPane_Main.razor # Main component file
```

### 🧩 When to Use Each Pattern

#### **Use Underscores in File Names When:**
- Component represents a **part** of a larger component
- Clear hierarchical relationship exists
- Want to maintain visual grouping in file lists

#### **Use Directories When:**
- Component **contains** other components
- Need to organize multiple related files
- Component is substantial enough to warrant its own namespace

#### **Use Underscore in Directory Names When:**
- Directory represents a **component part** (not just an organizational grouping)
- Need to maintain naming consistency with sibling components
- Directory contains sub-components of that specific part

### 📝 Naming Decision Tree
```
Is this a standalone component?
├─ Yes → Single file (Button.razor)
└─ No → Does it have parts?
    ├─ Yes → Use underscores (LeftPane_Header.razor)
    └─ Do parts have sub-components?
        ├─ Yes → Create directory (RightPane_Main/)
        └─ No → Keep as files with underscores
```

### ✅ Pattern Examples

#### **Correct Usage**
```
✅ LeftPane_Header.razor                    # Component part (file)
✅ LeftPane_Main.razor                      # Component part (file)
✅ RightPane_Main/                          # Component part (directory)
   ├── AsidePane/                           # Sub-component (directory)  
   └── RightPane_Main.razor                 # Component part (file)
```

#### **What This Avoids**
```
❌ LeftPaneHeader.razor                     # Loses visual hierarchy
❌ RightPaneMain/                           # Inconsistent with siblings
❌ RightPane_Main_AsidePane.razor           # Over-nested naming
```

### 🎨 Benefits of This System

1. **Visual Hierarchy**: File names immediately show component relationships
2. **Namespace Clarity**: Avoids naming conflicts while maintaining readability
3. **Scalable**: Works for simple to mega-complex component structures
4. **Self-Documenting**: Structure reveals component architecture
5. **Consistent**: Same pattern applied across file and directory names
