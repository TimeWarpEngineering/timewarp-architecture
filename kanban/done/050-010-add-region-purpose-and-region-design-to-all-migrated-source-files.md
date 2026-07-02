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
- [x] Inventory: 168 non-trivial candidates (excluded markers, global-usings, generated, <12-line
      files, and the 4 already done).
- [x] Grouped path-ordered into 14 slices of ~12 files for project-affine context.

### Phase 2: Add Regions
- [x] 14-agent workflow backfilled **160 files** (commit `98c63180`; 1,861 insertions, **0 deletions**
      — verified insertion-only via `git diff --numstat`).
- [x] **8 skipped with recorded reasons**: dead commented-out entities (`category.cs`, `product.cs`),
      `#if false` body (`user-claims-base.cs`), sub-threshold stubs (`greeter-service.cs`,
      `SideNavigationLink.razor.cs`, `assembly-extensions.cs`), and files whose single design fact
      was already an inline comment (`constants.cs`, `java-script-interop-constants.cs`).
- [x] Rules enforced in agent prompts: why-not-what, no temporal language, insertion-only, ≤14 lines.

### Phase 3: Verify
- [x] `dev build` green (0/0) — regions coexist with source-generated partials.
- [x] Random spot-checks carry genuine *why* (e.g. service-uri-provider: browser can't read Aspire
      env vars → server exposes /service-discovery; policy-registration: explicit always-true
      Anonymous policy avoids special-casing).

## Result

Region coverage went from 4/304 to 164/304 `.cs` files, with the remainder deliberately trivial.
Freshness from here on is the `AGENTS.md` maintenance rule; presence enforcement via an analyzer
remains a candidate follow-up (see Notes).

## Notes

- Ongoing freshness is covered by the maintenance rule in `AGENTS.md` (included by CLAUDE.md):
  every edit reconciles the file's regions. This task is the one-time backfill.
- Candidate follow-up (own task): a `TWPA` analyzer in `timewarp-architecture-contract-analyzers`
  flagging non-trivial files without `#region Purpose` — presence enforcement, same lesson as the
  nullability analyzer.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)
