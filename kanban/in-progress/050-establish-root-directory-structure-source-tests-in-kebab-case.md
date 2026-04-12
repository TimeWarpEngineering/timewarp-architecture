# Establish root directory structure (source, tests in kebab-case)

## Parent
047_migrate-timewarparchitecture-to-root

## Summary

Establish the root directory structure matching other TimeWarpEngineering repos. Move `TimeWarp.Architecture/Source/` contents to `source/` and `TimeWarp.Architecture/Tests/` contents to `tests/`, both using kebab-case naming.

## Current State

```
timewarp-architecture/dev/
├── TimeWarp.Architecture/
│   ├── Source/          ← contains: Analyzers, Common, ContainerApps, Libraries, GenTester
│   └── Tests/           ← contains: Analyzers.Tests, Common.Tests, ContainerApps.Tests, Libraries.Tests
├── TimeWarp.Templates/
├── tools/               ← dev-cli (already here!)
├── source/              ← empty (just Directory.Build.props)
└── ...
```

## Target Structure

```
timewarp-architecture/dev/
├── source/              ← main projects (moved from TimeWarp.Architecture/Source/)
│   ├── analyzers/       ← TimeWarp.Architecture.Source.Analyzers
│   ├── common/          ← Common.Domain, Common.Contracts, etc.
│   ├── container-apps/  ← Api, Web, Grpc, Yarp, Aspire
│   ├── libraries/       ← TimeWarp.Modules, etc.
│   └── ...
├── tests/               ← test projects (moved from TimeWarp.Architecture/Tests/)
│   ├── analyzers.tests/
│   ├── common.tests/
│   └── ...
├── tools/               ← dev-cli
└── ...
```

## Checklist

### Phase 1: Design
- [x] Analyze current structure (done in task 049)
- [x] Determine target structure based on other TimeWarp repos
- [x] Confirm migration order (leaf projects first)

### Phase 2: Create Directory Structure
- [x] source/ already exists
- [x] Create tests/ directory
- [x] Design subdirectory names (kebab-case)

### Phase 3: Migrate Projects (leaf-to-root order)
- [ ] Migrate from TimeWarp.Architecture/Source to source/
- [ ] Migrate from TimeWarp.Architecture/Tests to tests/
- [ ] Update project references (csproj paths)
- [ ] Update solution file (.slnx) references

### Child Tasks

- [ ] 052: Migrate TimeWarp.Modules to source/libraries/timewarp-modules/

### Phase 4: Verification
- [ ] Verify build succeeds
- [ ] Run tests
- [ ] Clean up old directories

## Notes

- **Migration order**: Based on dependency analysis in task 049, migrate leaf projects first (those with no dependents), then work up the dependency chain
- **Project references**: Will need to update `<ProjectReference Include="...">` paths in .csproj files
- **Solution files**: TimeWarp.Architecture.slnx will need updated paths
- **Namespaces**: Will remain unchanged (no coupling to folder structure per task 047 notes)

## References

- Task 048: dev-cli created (done)
- Task 049: Dependency analysis completed (41 projects, 8 phases identified)
- Dependency graph results: `.agent/workspace/dependency-graph.md`
- Other TimeWarp repos use: `src/`, `tests/`, `tools/`, `docs/`