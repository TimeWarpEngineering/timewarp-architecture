# Epic: Migrate TimeWarp.Architecture to root

> This is an Epic containing child tasks for migrating the TimeWarp.Architecture template to the root of the repository.

## Description

Migrate the TimeWarp.Architecture template project from its current subdirectory location to the root of the repository. This involves restructuring directories, updating paths, and ensuring all references are updated correctly.

## Child Tasks

- [x] 048: Create dev-cli and migrate ps1 scripts to Nuru runfiles (in-progress)
- [ ] 049: Analyze dependency graph to identify leaf projects
- [ ] 050: Establish root directory structure (source, tests in kebab-case)

## Checklist

- [x] Scaffold dev-cli via `ganda repo enforce-dev-cli --fix`
- [ ] Analyze project dependency graph
- [ ] Create root directory structure
- [ ] Migrate projects leaf-to-root
- [ ] Verify build succeeds
- [ ] Update documentation

## Notes

- Namespaces remain unchanged (no coupling to folder structure)
- Directory naming: `kebab-case` (e.g., `source/`, `tests/`, `tools/`)
- Migration order: leaf projects first, working up dependency chain
