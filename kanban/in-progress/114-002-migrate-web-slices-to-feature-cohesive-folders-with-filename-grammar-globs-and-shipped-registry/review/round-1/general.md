# Round 1 — general
**Date:** 2026-07-22
**Scope reviewed:** 8eae0006..9c96193f axis-1 migration

## Summary

Axis-1 ships as claimed: cohesive `web/features/<slice>/` with grammar renames, hybrid layer globs + Link metadata, registry JSON → `.g.cs` / `.g.props` + drift test, TWA0015/16 with `..` collapse and AnalyzerReleases, per-layer `features/` trees removed, SPA untouched, namespaces unchanged (TWA0009 remains path-independent). Sample inventory (hello, admin/roles 6/6/1, identity 14/16/1, IVT at web-contracts root) matches the plan; no dual-membership leftovers observed under current layer suffixes.

The MSBuild half of the “single registry, no hand-duplication” requirement is only partially met (props list is generated; globs / match / nesting lint still hardcode the five layers). Analyzer path scoping fixes the spike’s `web-server/../features` pitfall for the tested forms, but still treats bare `features/` as cohesive after collapse, which collides with SPA project-relative paths in principle. Docs/skills largely updated; a few pre-grammar filename examples remain in the contracts skill workflow.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/msbuild/feature-membership.targets:18-38,62-72,81-89
- Description: Task Requirements require the membership guard and analyzer both come from / be verified against one registry (no spike-style hand-duplication). The generator emits `FeatureFilenameGrammarLayer` / `FeatureFilenameGrammarFunction` into `feature-filename-grammar.g.props`, but the targets still hand-list every `Compile` glob (`*-contracts.cs` … `*-infrastructure.cs`), the zero-match `EndsWith` conditions, and the suffix-nesting lint. Generated function items are unused. Drift test only asserts JSON ↔ compiled `FeatureFilenameGrammar` ↔ props text — not that `feature-membership.targets` agrees. Adding a layer to the JSON would update the error-message layer list while globs/match stay stale (orphan or mis-hosted files).
- Suggestion: Generate the hybrid `Compile` ItemGroups and the match/nesting checks from the same registry (or batch over `@(FeatureFilenameGrammarLayer)`), and extend the drift test to assert membership targets contain a glob/match arm per registered layer.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs:161-210
- Description: Spike path pitfall is fixed for `../features/…`, `web-server/../features/…`, and absolute `…/web/features/…` (tests cover those). After collapse, any path that merely starts with `features/` or contains `/features/` and is not a hard-coded `/web-*/features/` marker is treated as the cohesive tree. SPA still lives at `web-spa/features/` and, when Roslyn supplies a project-relative path of the form `features/…` (no `web-spa/` segment), layer-marker exclusion never runs. Current SPA filenames lack registered layer suffixes so they stay silent today (`NotApplicable`), but a grammar-shaped SPA name (or design-time path form) would get TWA0015/16 false positives. Tests claim layer-project silence only for `web-application/features/…` and absolute `…/web-server/features/…`, not SPA-relative `features/…`.
- Suggestion: Prefer affirmative cohesive markers (`/web/features/`, `../features/` from layer projects only) over the broad “any remaining `/features/`” fallback; add a regression test that `features/counter/…` and absolute `…/web-spa/features/…` stay silent even for `*-handler-application.cs` stems.
- Status: open

### Issue 3 — Severity: nit
- File: skills/tw-web-api-contracts/SKILL.md:218,235
- Description: Axis-1 layout docs and canonical examples use `*-contracts.cs` under `web/features/`, but workflow steps still say `queries/get-*.cs` / `commands/create-|update-|delete-*.cs` and `role-details.cs` without the layer suffix. Agents following only those lines will reintroduce pre-migration names that fail the membership guard.
- Suggestion: Align workflow examples with the grammar (`get-*-contracts.cs`, `role-details-contracts.cs`).
- Status: open

## Non-issues verified

- No leftover product `.cs` under `web-contracts/features`, `web-application/features`, or `web-server/features` (folders gone).
- Hybrid includes are project-name gated; SPA does not pull cohesive globs.
- IVT lives at `web-contracts/internals-visible-to-client-and-server.cs` (not under the feature tree).
- TWA0015/16 present in `AnalyzerReleases.Unshipped.md`; AGENTS.md TWA table + layout + rebuild caveat present.
- Escape hatch (`role-store-application.cs`, `web-authn-*-application.cs`) and contracts collapse form are covered by tests.
- Template smoke / full `dev test` deferred as claimed — not re-run here.
