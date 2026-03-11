# Epic: Migrate TimeWarp.Architecture to root

> This is an Epic containing child tasks for migrating the TimeWarp.Architecture template to the root of the repository.

## Description

Migrate the TimeWarp.Architecture template project from its current subdirectory location to the root of the repository. This involves restructuring directories, updating paths, and ensuring all references are updated correctly.

## Child Tasks

- [ ] 047-001: Create dev-cli and migrate ps1 scripts to Nuru runfiles
- [ ] 047-002: Analyze dependency graph to identify leaf projects
- [ ] 047-003: Establish root directory structure (source, tests in kebab-case)

## Checklist

- [ ] Plan migration approach
- [ ] Execute migration
- [ ] Verify build succeeds
- [ ] Update documentation

## Notes

[Additional context]
