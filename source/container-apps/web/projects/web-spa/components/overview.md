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
- `StatusBadge.razor`
- `FormContainer.razor`

**Inline status:** use FluentUI `FluentMessageBar` (not a custom alert). Buttons: `FluentButton`.

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
