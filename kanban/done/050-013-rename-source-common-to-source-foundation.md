# Rename source/common/ to source/foundation/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Rename the `source/common/` directory to `source/foundation/` to align with the `foundation-*` project naming and the planned `TimeWarp.Foundation.*` NuGet namespace (task 051). `common/` is a meaningless bucket name; `foundation/` says what these projects are.

## Rationale

- **Consistency** — Projects inside are already named `foundation-contracts`, `foundation-domain`, `foundation-application`, `foundation-infrastructure`, `foundation-server`
- **Forward alignment** — Task 051 will rename namespaces to `TimeWarp.Foundation.*`; the directory should match
- **Clarity** — `foundation/` communicates purpose (foundational shared layer); `common/` communicates nothing
- **Convention** — Other TimeWarpEngineering repos use purposeful directory names, not generic ones

## Current State

```
source/
├── common/                          ← rename to foundation/
│   ├── Directory.Build.props         ← chains to source/, NoWarn suppressions
│   ├── foundation-contracts/
│   ├── foundation-domain/
│   ├── foundation-application/
│   ├── foundation-infrastructure/
│   └── foundation-server/
├── libraries/
├── analyzers/
└── container-apps/
```

## Target State

```
source/
├── foundation/                       ← renamed
│   ├── Directory.Build.props         ← update if needed
│   ├── foundation-contracts/
│   ├── foundation-domain/
│   ├── foundation-application/
│   ├── foundation-infrastructure/
│   └── foundation-server/
├── libraries/
├── analyzers/
└── container-apps/
```

## Skills

- csharp

## Checklist

### Phase 1: Rename Directory
- [x] `git mv source/common source/foundation`
- [x] Verify all files moved correctly

### Phase 2: Update ProjectReference Paths
All projects that reference foundation-* projects use relative paths with `common/` in them. These need updating to `foundation/`. Projects to update:
- [x] All 5 foundation-* .csproj files (sibling references use `..\foundation-*\`, unaffected)
- [x] source/container-apps/ projects referencing foundation-* (e.g., grpc-domain, web-domain reference foundation-domain)
- [x] TimeWarp.Architecture/Source/ projects referencing foundation-* (many ContainerApps projects)
- [x] TimeWarp.Architecture/Tests/ projects referencing foundation-*

### Phase 3: Update Solution Files
- [x] Update timewarp-architecture.slnx — change `/source/common/` folder path to `/source/foundation/`
- [x] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect all 5 foundation project paths

### Phase 4: Verify
- [x] Build verify timewarp-architecture.slnx
- [x] Build verify TimeWarp.Architecture/TimeWarp.Architecture.slnx (if it resolves correctly)

## Notes

### Scope of Path Changes
This is a wide-impact rename — every project that references a foundation-* project has `common/` in its relative path and needs it changed to `foundation/`. Use rg to find all occurrences:
```
rg "source.common.foundation" --glob "*.csproj" .
rg "source\\\\common\\\\foundation" --glob "*.csproj" .
```

### Directory.Build.props
The `source/foundation/Directory.Build.props` chains to `source/Directory.Build.props` via `GetPathOfFileAbove`. This path resolution is relative and should work unchanged after the rename.

### Why Not Wait Until 051?
Doing this now (before more projects are migrated) reduces the blast radius — fewer referencing projects to update. Each additional migration adds more paths containing `common/` that would need changing later.

## Results

Successfully renamed source/common/ to source/foundation/.

### What was implemented
- `git mv source/common source/foundation` — renamed directory
- Updated 17 csproj files (ProjectReference and AdditionalFiles paths)
- Updated 2 slnx files (project paths and folder name)
- Simple find-and-replace: `common\foundation` → `foundation\foundation`, `common/foundation` → `foundation/foundation`
- Git detected all 65 file renames at 100% similarity (history preserved)

### Files changed
- source/common/ → source/foundation/ (65 files renamed)
- 15 ContainerApps csproj files (path updates: common → foundation)
- 2 source/container-apps csproj files (path updates)
- 1 Tests csproj file (path update)
- timewarp-architecture.slnx (folder name + project paths)
- TimeWarp.Architecture/TimeWarp.Architecture.slnx (project paths)

### Key decisions
- Did the rename now before more projects are migrated — reduces blast radius
- Simple sed replacement on path strings — no manual path computation needed
- Directory.Build.props chain still works (relative path resolution unchanged)

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors (13 projects)

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)
- Completed: ses_25164d685ffewz6L5iK8OxXmYQ (2026-04-21)