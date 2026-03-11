# Analyze dependency graph to identify leaf projects

## Parent
047_migrate-timewarparchitecture-to-root

## Description

Analyze the project dependency graph to identify leaf projects (projects with no internal dependencies). This is crucial for understanding the migration order and ensuring we can safely move projects to the root without breaking dependencies.

## Status

**Complete** - Dependency graph documented in `dependency-graph.md`

## Checklist

- [x] Generate dependency graph
- [x] Identify leaf projects (10 leaf projects identified)
- [x] Document dependencies (41 total projects mapped)
- [x] Create migration order plan (8 phases defined)

## Summary

### Leaf Projects (Phase 1 - Migrate First)
1. Common.Contracts
2. Common.Domain
3. TimeWarp.Architecture.Attributes
4. TimeWarp.Architecture.Analyzers
5. TimeWarp.Modules
6. TimeWarp.Automation.Contracts
7. Api.Domain
8. Grpc.Domain
9. Web.Domain
10. Aspire.ServiceDefaults

### Top-Level Apps (Phase 6 - Migrate Last)
- Web.Spa
- Aspire.AppHost

### Total Projects: 41
- Source projects: 30
- Test projects: 10
- Utility projects: 1 (GenTester)

## Notes

See `dependency-graph.md` for full dependency analysis and migration order.
