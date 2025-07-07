# Component Hierarchy Naming Convention Overview

**Date**: 2025-01-07  
**Scope**: TimeWarp.Architecture Web.Spa Components  
**Status**: Discovered Pattern - Needs Documentation  

## 🎯 Executive Summary

The TimeWarp.Architecture project has evolved a **sophisticated component hierarchy naming system** that uses underscores to represent component relationships while maintaining clear organizational structure. This system is **consistent and logical** but undocumented.

## 🔍 The Component Hierarchy Naming Pattern

### **Core Principle**
Underscores in component names represent **hierarchical relationships** and are used consistently across both files and directories when those directories represent component containers.

### **Pattern Rules**

#### **1. Simple Components** (No hierarchy)
```
Button.razor                    # Single, standalone component
Text.razor                      # Single, standalone component
```

#### **2. Component Parts** (File-level hierarchy)
```
ComponentName_PartName.razor
```
Examples:
- `LeftPane_Header.razor`
- `LeftPane_Main.razor`
- `LeftPane_Footer.razor`

#### **3. Component Containers** (Directory-level hierarchy)
When a component part contains sub-components, it becomes a directory:
```
ComponentName_PartName/         # Directory for component part
├── SubComponent1/
├── SubComponent2/
└── ComponentName_PartName.razor # The main component file
```

Example:
```
RightPane_Main/                 # Container directory
├── AsidePane/                  # Sub-component directory
├── PagePane/                   # Sub-component directory  
└── RightPane_Main.razor        # Main component file
```

## 📚 Organizational Tiers

### **Tier 1: Simple Components**
**Pattern**: Single files  
**Location**: `Elements/`, `Forms/`, etc.  
**Examples**:
- `Button.razor`
- `HyperLink.razor`
- `FormContainer.razor`

### **Tier 2: Complex Components** 
**Pattern**: Flat structure with underscore hierarchy  
**Location**: Component subdirectories  
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

### **Tier 3: Mega Components**
**Pattern**: Nested directories + underscore hierarchy  
**Location**: Complex component structures  
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

## 🧩 When to Use Each Pattern

### **Use Underscores in File Names When:**
- Component represents a **part** of a larger component
- Clear hierarchical relationship exists
- Want to maintain visual grouping in file lists

### **Use Directories When:**
- Component **contains** other components
- Need to organize multiple related files
- Component is substantial enough to warrant its own namespace

### **Use Underscore in Directory Names When:**
- Directory represents a **component part** (not just an organizational grouping)
- Need to maintain naming consistency with sibling components
- Directory contains sub-components of that specific part

## ✅ Pattern Validation Examples

### **Correct Usage**
```
✅ LeftPane_Header.razor                    # Component part (file)
✅ LeftPane_Main.razor                      # Component part (file)
✅ RightPane_Main/                          # Component part (directory)
   ├── AsidePane/                           # Sub-component (directory)  
   └── RightPane_Main.razor                 # Component part (file)
```

### **What This Avoids**
```
❌ LeftPaneHeader.razor                     # Loses visual hierarchy
❌ RightPaneMain/                           # Inconsistent with siblings
❌ RightPane_Main_AsidePane.razor           # Over-nested naming
```

## 🎨 Benefits of This System

1. **Visual Hierarchy**: File names immediately show component relationships
2. **Namespace Clarity**: Avoids naming conflicts while maintaining readability
3. **Scalable**: Works for simple to mega-complex component structures
4. **Self-Documenting**: Structure reveals component architecture
5. **Consistent**: Same pattern applied across file and directory names

## 📝 Implementation Guidelines

### **For New Components**

1. **Start Simple**: Single file if no sub-parts needed
2. **Add Underscores**: When component has logical parts
3. **Create Directories**: When parts need their own sub-components
4. **Maintain Consistency**: Use underscores in directory names for component parts

### **Naming Decision Tree**
```
Is this a standalone component?
├─ Yes → Single file (Button.razor)
└─ No → Does it have parts?
    ├─ Yes → Use underscores (LeftPane_Header.razor)
    └─ Do parts have sub-components?
        ├─ Yes → Create directory (RightPane_Main/)
        └─ No → Keep as files with underscores
```

## 🔄 Migration Strategy

1. **Document Current Pattern**: Create ADR for this naming system
2. **Fix Anomalies**: Move misplaced components to correct locations
3. **Apply Consistently**: Use pattern for all new component development
4. **Update Guidelines**: Include in development documentation

## 📖 Conclusion

The component hierarchy naming system using underscores is a **sophisticated and logical approach** that has evolved organically in the codebase. Rather than being inconsistent, it represents a mature solution to component organization challenges.

**This pattern should be preserved, documented, and applied consistently going forward.**