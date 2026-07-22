# Migrate web slices to feature-cohesive folders with filename-grammar globs and shipped registry

## Description

Executes the axis-1 decision (114 `axis-decisions.md`, spike-validated in
[[114-001-axis-1-spike-convert-one-slice-to-filename-grammar-globs-and-validate-tooling]] — Steve:
GO, 2026-07-22): move the web container-app's product slices into the feature-cohesive tree
`source/container-apps/web/features/<slice>/`, with layer projects (contracts / application /
server) composed by static filename-grammar globs `<name>[-<function>]-<layer>.cs`. Spa stays
conventional (axis-1 rule); assembly granularity unchanged (axis 2: single per layer).

## Requirements (findings from the spike are binding)

- **Grammar registry is SINGLE-SOURCE**: one registry artifact defines function→layer pairs
  (`handler`→application, `endpoint`→server, `feature-annotations`→server [Steve 2026-07-22],
  plus whatever migration surfaces) and the layer-suffix set; BOTH the MSBuild membership guard
  and the analyzer are generated from / verified against it (two-things-must-agree; no
  hand-duplication like the spike's TWA9999 prototype).
- **Membership guard ships**: zero-match → teaching build error; registry-suffix-nesting lint
  (structural dual-match prevention). Central import, runs once.
- **Archetype analyzer ships as a real TWA rule** (number assigned from the TWA sequence, not
  TWA9999): function↔layer pairing + unknown-function error, teaching-quality diagnostics.
  MUST encode the spike's path pitfall: FilePath arrives project-relative WITH `..` traversal —
  normalize or match `<proj>/features/` shapes; regression-test both path forms.
- **Contracts files** collapse function segment (`<name>-contracts.cs`); escape hatch
  `<name>-<layer>.cs` (no function) remains valid for non-archetype files.
- Namespaces are NOT renamed by this migration (namespaces-don't-track-folders rule); TWA0009
  slice isolation must keep passing unchanged.
- Hybrid include option 1 (default SDK items + cross-folder globs + Link metadata) per the
  spike; old per-layer `features/` folders removed as slices move.
- Docs: AGENTS.md layout section, slice-isolation + web-api-contracts skills updated for the
  new file locations/grammar; registry-change ⇒ rebuild caveat documented (incremental
  staleness finding).
- Template: dotnet-new output remains valid (flags strip within the cohesive tree per spike Q5);
  template smoke with a flag off.

## Checklist

- [ ] Inventory slices to migrate (hello, identity, admin/roles, users, …) — enumerate from the
      three layer projects' `features/` folders; migrate slice-by-slice, build green after each
- [ ] Registry artifact + generation/verification wiring (guard + analyzer from one source)
- [ ] Membership guard promoted from spike shape to shipped location + tests
- [ ] Archetype analyzer as real TWA rule (path-normalization + both-path-form tests;
      AnalyzerReleases entries)
- [ ] Migrate all web slices with grammar renames; delete emptied per-layer features folders
- [ ] `dev build` 0/0 + full `dev test` green; TWA0009 unchanged; template both-ways smoke
- [ ] Docs/skills reconciled (AGENTS.md, slice-isolation, web-api-contracts)
- [ ] Fold outcome into 114 (unblocks the ADR)

## Notes

- Spike branch `spike/axis1-filename-globs` (local, `04e5b2c8`) is the reference implementation
  for the mechanics; do not merge it — re-implement at ship quality.
- Watch-item from spike: intermittent `-t:Rebuild` "1 Error" in piped output (never reproducible
  captured; suspected console race) — note if observed again.
- New-file misplacement is caught at build (membership guard + TWA0004), not creation — docs note.

## Session

- Created: 2026-07-22 (specced from 114-001 findings per DoR; Steve gated GO)
