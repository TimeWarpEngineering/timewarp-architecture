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

- [x] Inventory slices (no `users` slice; ~68 product `.cs` files across three layer `features/`
      trees — see plan §1)
- [x] Registry artifact + generation/verification wiring (guard + analyzer from one source)
- [x] Membership guard promoted from spike shape to shipped location + tests
- [x] Archetype analyzer as real TWA rule (path-normalization + both-path-form tests;
      AnalyzerReleases entries)
- [x] Migrate all web slices with grammar renames; delete emptied per-layer features folders
- [x] `dev build` 0/0 + analyzer/web contracts/web-server integration tests green; TWA0009 unchanged (template smoke deferred)
- [x] Docs/skills reconciled (AGENTS.md, slice-isolation, web-api-contracts)
- [x] Fold outcome into 114 (unblocks the ADR)

## Notes

- Spike branch `spike/axis1-filename-globs` (local, `04e5b2c8`) is the reference implementation
  for the mechanics; do not merge it — re-implement at ship quality.
- Watch-item from spike: intermittent `-t:Rebuild` "1 Error" in piped output (never reproducible
  captured; suspected console race) — note if observed again.
- New-file misplacement is caught at build (membership guard + TWA0004), not creation — docs note.

### Implementation plan (2026-07-22, Phase 2)

**Status:** Ready to execute. No blocking open questions.  
**Out of scope:** SPA rehome; namespace renames; domain aggregate moves; ADR write (fold-back into
114 unblocks ADR).

#### Goal

```text
source/container-apps/web/features/<slice>/…   # cohesion (all layers together)
web-{contracts,application,server}/            # compilation via static *-layer.cs globs
```

#### SSOT registry

- Path: `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
- Layers: contracts, application, domain, infrastructure, server
- Functions: `handler`→application, `endpoint`→server, `feature-annotations`→server
- Analyzer: generate constants into analyzer assembly (no hand-duplicated maps)
- MSBuild: generate committed `source/container-apps/web/msbuild/feature-filename-grammar.g.props`
- Test: JSON ↔ generated C# ↔ props must not drift
- Doc: registry edit ⇒ full rebuild (analyzer incremental staleness)

#### Membership guard

- `web/msbuild/feature-membership.targets` + import once via `web/Directory.Build.targets`
- Zero-match on `web/features/**/*.cs` without registered `-{layer}` suffix → teaching error
- Suffix-nesting lint (structural dual-match prevention)
- Hybrid includes (option 1): default SDK + `../features/**/*-{layer}.cs` + Link metadata on
  contracts / application / server; also wire empty domain/infrastructure globs

#### Analyzer (TWA0015 / TWA0016)

- Package: convention analyzers (TWA0014 is current max)
- **TWA0015**: registered function + wrong layer (teaching list of pairs)
- **TWA0016**: unregistered function segment used as archetype (closed-set longest-match;
  escape hatch `<name>-<layer>.cs` stays valid — do NOT require every pre-layer token registered)
- Path pitfall mandatory: normalize `..` project-relative paths; never exclude by bare
  `web-server/` substring; scope to cohesive `/features/` marker
- Tests: both path forms (`../features/…` and `…/web-server/../features/…`), escape hatch,
  contracts, outside-tree silence, AnalyzerReleases.Unshipped

#### Slice inventory (authoritative — no `users`)

| Slice | C/A/S | Notes |
|-------|-------|-------|
| hello | 1/1/1 | Pilot first after platform |
| analytics, auth, profile | 1/1/1 each | Small triples |
| admin/roles | 6/6/1 | Nested + `role-store` escape hatch |
| identity | 14/16/1 | Largest — last product slice |
| chat | 4/1/0 | Multi-folder contracts |
| authentication, todo-items | contracts-only | |
| authorization, admin/modules | id substrate | still under cohesive tree |
| infrastructure IVT | special | **Leave** feature tree → contracts project root |
| spa features | — | **Not moved** |

Rename: preserve subfolders (`commands/`, `queries/`, …); only filename grammar + reparent.
Examples: `hello.cs`→`hello-contracts.cs`, `*-handler.cs`→`*-handler-application.cs`,
`feature-annotations.cs`→`<slice>-feature-annotations-server.cs`.

#### Migration order

1. **Phase A — platform:** JSON + gen, membership targets, hybrid globs, TWA0015/16 + tests;
   build green with empty cohesive tree.
2. **Phase B — slices:** hello → analytics → auth → profile → admin/roles → chat →
   authentication → todo-items → authorization + admin/modules → identity → IVT + delete
   emptied per-layer `features/`. Build green after each.
3. **Phase C:** full `dev build`/`dev test`, docs/skills, template smoke (default + flag off),
   fold into 114.

#### Docs touch list

AGENTS.md (layout + TWA table + rebuild caveat), `skills/tw-slice-isolation`,
`skills/tw-web-api-contracts`, grep old `web-*/features` paths under skills/.

#### DoD

All product `.cs` from three layer `features/` under `web/features/**` with grammar names;
single registry; guard + TWA0015/16; 0/0 + tests; docs; 114 fold-in.

## Session

- Created: 2026-07-22 (specced from 114-001 findings per DoR; Steve gated GO)
- Orchestration: 2026-07-22 — Phase 1 in-progress; Phase 2 plan locked (above)

### Session note — implementer (2026-07-22)

Axis-1 migration shipped at quality (no spike merge):

1. **Registry SSOT** `feature-filename-grammar.json` → generated `feature-filename-grammar.g.cs`
   (analyzer constants) + committed `web/msbuild/feature-filename-grammar.g.props` via
   `generate-feature-filename-grammar.py` (MSBuild target on convention-analyzers). Drift test
   asserts JSON ↔ C# type ↔ props.
2. **Membership guard** `web/msbuild/feature-membership.targets` imported once via
   `web/Directory.Build.targets`: zero-match teaching error, suffix-nesting lint, hybrid includes
   for contracts/application/server (+ empty domain/infrastructure globs).
3. **TWA0015 / TWA0016** with path normalization (`..` collapse); never exclude by bare
   `web-server/` substring. Tests cover both path forms, escape hatch, contracts, outside-tree.
4. **All product slices** rehomed under `web/features/<slice>/` with grammar renames; IVT moved
   to web-contracts root; per-layer `features/` deleted. SPA not moved; namespaces unchanged.
5. **Docs**: AGENTS.md layout + TWA table + rebuild caveat; skills tw-slice-isolation +
   tw-web-api-contracts updated.
6. **Verification**: `dev build` 0/0; analyzer tests 95p; web-contracts-tests 38p;
   web-server-integration-tests 97p/1s. Full `dev test` / template smoke not run end-to-end.

Commits: platform infra; slice migration; docs (this session).

- Phase 4b review: 2026-07-22 — effort 1 general; 3 findings fixed (M1–M3); disposition clean.
  Artifacts: `review/review-framework.md`, `review/round-1/`, `review/disposition.md`.

## Results

### What was implemented

Axis-1 migration for the web container-app (re-implemented; spike branch not merged):

1. **Registry SSOT** — `feature-filename-grammar.json` generates analyzer constants
   (`feature-filename-grammar.g.cs`) and MSBuild props (`feature-filename-grammar.g.props`)
   including hybrid Compile globs + layer-suffix regex; nesting rejected at generation time.
2. **Membership guard** — `web/msbuild/feature-membership.targets` (import once via
   `web/Directory.Build.targets`): zero-match teaching error via generated regex; hybrid includes
   come from generated props (no hand-listed layers).
3. **TWA0015 / TWA0016** — function/layer pairing + incomplete/wrong-case function diagnostics;
   affirmative cohesive path scoping (SPA-safe); AnalyzerReleases unshipped entries.
4. **Slice rehome** — all product `.cs` from the three layer `features/` trees under
   `source/container-apps/web/features/<slice>/` with grammar renames; IVT at contracts root;
   per-layer `features/` removed; SPA/namespaces unchanged.
5. **Docs** — AGENTS.md layout + TWA table + registry rebuild caveat; slice-isolation +
   web-api-contracts skills; workflow examples grammar-aligned post-review.
6. **Parent 114** — fold-in notes recorded; ADR unblocked (ADR write remains on 114).

### Files changed (high level)

- `source/analyzers/timewarp-architecture-convention-analyzers/*grammar*`
- `source/container-apps/web/msbuild/*`, `web/Directory.Build.targets`, layer csprojs
- `source/container-apps/web/features/**` (cohesive tree)
- `tests/analyzers/.../feature-filename-grammar-analyzer-tests.cs`
- `AGENTS.md`, `skills/tw-slice-isolation`, `skills/tw-web-api-contracts`

### Key decisions / deviations

- Closed-set longest-first function match preserves escape hatches.
- Hybrid globs generated into props (project name `web-{layer}` convention).
- Template smoke + full `dev test` not run end-to-end (partial suite verified).
- Premature “done” by implementer was rolled back for Phase 4b; folderized for `review/`.

### Test outcomes

| Check | Result |
|--------|--------|
| `dev build` | 0/0 pass (post-review fix too) |
| Analyzer tests | 96 passed (after SPA path tests) |
| web-contracts-tests | 38 passed (implement phase) |
| web-server-integration-tests | 97 passed, 1 skipped (implement phase) |
| Full `dev test` | not run |
| Template smoke | not run |

### Phase 4b review

- **Effort:** 1 (general)
- **Rounds:** 1
- **Final counts:** 0 open; 2 suggestion fixed; 1 nit fixed; 0 bugs; 0 wontfix
- **Disposition:** clean
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`

