# Add #region Purpose and #region Design to all migrated source files

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

After all projects are migrated to source/, add `#region Purpose` and `#region Design` blocks to every migrated .cs file per the csharp skill convention. This is deferred until after migration completes so we have full architectural context to write accurate regions.

## Rationale

- The csharp skill requires `#region Purpose` and `#region Design` in all source files
- Adding regions during migration would be premature — we're still learning the architecture
- Writing regions after full migration means we understand cross-project relationships
- One pass at the end is more efficient than per-task additions
- Avoids rework if understanding evolves during migration

## Scope

All .cs files under source/ that were migrated as part of task 050:
- source/common/ (foundation-contracts, foundation-domain, foundation-application)
- source/libraries/ (timewarp-modules, timewarp-automation-contracts)
- source/analyzers/ (timewarp-architecture-attributes, timewarp-architecture-analyzers)
- source/container-apps/ (api-domain, grpc-domain, web-domain, aspire-service-defaults)
- Plus any additional projects migrated before this task starts

## Skills

- csharp

## Checklist

### Phase 1: Inventory
- [ ] List all .cs files under source/ that need regions
- [ ] Identify files that already have regions (skip those)
- [ ] Categorize by project for systematic processing

### Phase 2: Add Regions
For each file:
- [ ] Add `#region Purpose` — one-line description of what the file/class does
- [ ] Add `#region Design` — key design decisions, constraints, rationale
- [ ] Add `#region Open Questions` only if there are genuine open questions
- [ ] Follow csharp skill format: concise, focus on "why" not "what"
- [ ] Skip trivial files per skill guidance

### Phase 3: Verify
- [ ] Build all projects — regions must not break compilation
- [ ] Review a sample of files for quality and consistency

## Notes

### Region Format (per csharp skill)
```
#region Purpose
// One-line description of what this file/class does
#endregion

#region Design
// Key design decisions and rationale
// Constraints and dependencies
// Why certain approaches were chosen
#endregion
```

### What Counts as "Trivial"
- Files with only a namespace and IAssemblyMarker interface
- Empty GlobalUsings.cs files
- Files under 10 lines with no logic

### Dependency
This task should not start until all 050-x migration subtasks are complete.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)
