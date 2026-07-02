# Add #region Purpose and #region Design to all migrated source files

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

All 050-x migration subtasks are complete; this is the deferred backfill pass. Add
`#region Purpose` and `#region Design` blocks to every non-trivial .cs file under `source/`, per
the **agent-context-regions** skill. Deferred until after migration so regions are written with
full architectural context.

Adoption at task start: **4 of 304** .cs files have `#region Purpose`; 0 have `#region Design`.

## Scope

All .cs files under `source/`:
- `source/foundation/` (foundation-contracts, -contracts-generators, -domain, -application,
  -infrastructure, -server)
- `source/libraries/` (timewarp-modules)
- `source/analyzers/` (timewarp-architecture-attributes, -analyzers, -contract-analyzers)
- `source/container-apps/` (web, api, grpc, aspire, yarp)

## Skills

- agent-context-regions (format, placement, maintenance rule, what counts as trivial)
- csharp (surrounding style)

## Checklist

### Phase 1: Inventory
- [ ] List all .cs files under source/; exclude trivial ones (assembly markers, global-usings,
      <10 lines with no logic, generated code) and the 4 already done.
- [ ] Group by project so each pass has whole-project context.

### Phase 2: Add Regions (per project, with cross-file context)
- [ ] `#region Purpose` — one line — and `#region Design` — the why: decisions, constraints,
      rationale (5–10 lines max) — at the top of each file, before the namespace.
- [ ] `#region Open Questions` only where a genuine question surfaced while reading.
- [ ] No temporal language; say what the code can't; skip anything that would restate the name.

### Phase 3: Verify
- [ ] `dev build` green (regions must not break compilation, incl. source-generated partials).
- [ ] Sample review for quality: regions must carry the *why*, not paraphrase the class name.

## Notes

- Ongoing freshness is covered by the maintenance rule in `AGENTS.md` (included by CLAUDE.md):
  every edit reconciles the file's regions. This task is the one-time backfill.
- Candidate follow-up (own task): a `TWPA` analyzer in `timewarp-architecture-contract-analyzers`
  flagging non-trivial files without `#region Purpose` — presence enforcement, same lesson as the
  nullability analyzer.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)
