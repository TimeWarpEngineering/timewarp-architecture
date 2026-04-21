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
- [ ] `git mv source/common source/foundation`
- [ ] Verify all files moved correctly

### Phase 2: Update ProjectReference Paths
All projects that reference foundation-* projects use relative paths with `common/` in them. These need updating to `foundation/`. Projects to update:
- [ ] All 5 foundation-* .csproj files (sibling references use `..\foundation-*\`, unaffected)
- [ ] source/container-apps/ projects referencing foundation-* (e.g., grpc-domain, web-domain reference foundation-domain)
- [ ] TimeWarp.Architecture/Source/ projects referencing foundation-* (many ContainerApps projects)
- [ ] TimeWarp.Architecture/Tests/ projects referencing foundation-*

### Phase 3: Update Solution Files
- [ ] Update timewarp-architecture.slnx — change `/source/common/` folder path to `/source/foundation/`
- [ ] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect all 5 foundation project paths

### Phase 4: Verify
- [ ] Build verify timewarp-architecture.slnx
- [ ] Build verify TimeWarp.Architecture/TimeWarp.Architecture.slnx (if it resolves correctly)

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

To be filled after completion.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)