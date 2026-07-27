# Round 1 — general
**Date:** 2026-07-27
**Scope reviewed:** commit `49a2d1c3` (platform/postgres + platform/identity-host clusters)

## Summary

Task 126-008 lands cleanly: a second grammar tree root (`WebPlatformTreeRoot`) is emitted by
`generate-feature-filename-grammar.py` into `feature-filename-grammar.g.props` (hybrid
`Compile` globs with `Link=platform\…`, not hand-edited ad-hoc includes), and
`feature-membership.targets` both defines the root and membership-scans `platform/**/*.cs`
when the directory exists. All 12 files sit at the planned paths with correct `-infrastructure` /
`-server` escape-hatch suffixes; namespaces remain non-`Features.*` (Persistence, Services,
Configuration, HostedServices, Modules). Seam interfaces stay in
`web-application/abstractions/`. Template `(!postgres)` excludes list all five new postgres paths.
AGENTS.md Layout + Axis-1 grammar and `skills/tw-feature-placement/SKILL.md` document
features vs platform vs host in present tense. Emptied layer subfolders under web-server /
web-infrastructure are gone. Stale product-path greps hit only historical `kanban/done/`
records.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/feature-filename-grammar-analyzer-tests.cs
  (SSOT drift assertion ~198–227)
- Description: The drift test asserts each layer appears as
  `FeatureFilenameGrammarLayer`, that props contain `**/*-{layer}.cs` and the per-project
  ItemGroup condition, and that membership imports the generated props + regex without
  hand-listing layer globs. It does **not** assert `WebPlatformTreeRoot` (in g.props or in
  membership.targets) nor that membership includes a second `FeatureTreeFile` scan of the
  platform tree. A future generator/membership regression that dropped the platform root while
  leaving feature globs intact would still pass this test — the critical mechanic this task
  added is not locked in the SSOT drift gate.
- Suggestion: Extend the drift test to require `WebPlatformTreeRoot` (and ideally
  `Link="platform\…"`) in the generated props, and `WebPlatformTreeRoot` + platform
  `FeatureTreeFile` include (or equivalent) in `feature-membership.targets`.
- Status: open

## Verification notes (no further issues)

- **Generator → g.props:** Python generator emits dual hybrid globs per layer; committed
  `feature-filename-grammar.g.props` matches (features + platform Link metadata, Condition on
  non-empty `WebPlatformTreeRoot`). No hand-only edits beyond that generator surface.
- **Membership:** `WebFeatureTreeRoot` + `WebPlatformTreeRoot` set; guard error text names both
  trees; platform scan gated on `Exists('$(WebPlatformTreeRoot)')`.
- **12 moves:**
  - `platform/postgres/` ×5 (2 infrastructure, 3 server)
  - `platform/identity-host/` ×7 (all server)
  - Layer project folders no longer hold the moved sources (web-infrastructure left with
    module + csproj; web-server with program/config samples only).
- **Namespaces:** no `TimeWarp.Architecture.Features.*` under `platform/`.
- **template.json `(!postgres)`:** five paths under
  `source/container-apps/web/platform/postgres/…` plus unchanged
  `ef-principal-store-infrastructure.cs` and web-infrastructure-tests exclude.
- **Docs:** AGENTS.md platform line + dual-tree Axis-1 blurb; skill table and membership
  wording cover features / platform / host; seam interfaces called out as staying put.
- **Stayers (out of scope, confirmed present):** four abstractions under
  `web-application/abstractions/`, `web-infrastructure-module.cs`, sample options/environment
  check, `program.cs`.
- **Purpose/Design regions:** no narration of old folder homes; identity-host Design text
  correctly names `platform/identity-host` where path is mentioned.
- **TWA0015/16 scope:** analyzer still features-only (by design); platform enforcement of
  missing/misspelled layer suffixes is the membership guard — consistent with task scope.
