# Chore: misc cleanup from folder review (DotSettings noise, redundant nullable directives)

## Description

Small debris found during Steve's post-126 folder review (2026-07-27). Closed scope — the items
below, not a perpetual dumping ground.

1. **Delete the two ReSharper `.DotSettings` files** — pure IDE noise shipping as template
   content:
   - `source/container-apps/web/web-spa/web-spa.csproj.DotSettings`
   - `source/container-apps/web/web-contracts/web-contracts.csproj.DotSettings`
   Before deleting, skim each for any setting that encodes a real convention (if one does,
   surface it — the convention belongs in `.editorconfig`/analyzers, not per-IDE files).

2. **Remove redundant `#nullable enable` directives from hand-written files.**
   `<Nullable>enable</Nullable>` is global (`Directory.Build.props:38`); 18 files carry the
   directive (grep `#nullable enable` under source/ + tests/, excluding obj/).
   **CARVE-OUT (do not touch):**
   - Generator *emitter* sources where `#nullable enable` is part of the EMITTED text
     (e.g. `typed-id-source-generator.cs`, `mock-response-factory-registry-generator.cs`,
     `ingress-route-prefix-generator.cs` — verify by reading whether the directive is in a
     string literal/raw string being generated, not at file top level).
   - Generated files (`*.g.cs`, e.g. `feature-filename-grammar.g.cs`) — regenerated output
     conventionally carries the directive; fixing it means changing the GENERATOR's emission,
     which is out of scope here.
   Only file-top-level directives in hand-written `.cs` files are removed.

## Checklist

- [x] Skim + delete both `.DotSettings` files (surface any real-convention setting found)
- [x] Classify all 18 `#nullable enable` hits: hand-written top-level vs emitter-string vs
      `.g.cs`; remove only the first category
- [x] `dev build` 0/0 (nullable warnings-as-errors will catch any file that actually depended
      on a local directive), `dev test`
- [x] `dev template-smoke` both matrices (template content changed — file deletions)

## Notes

- Parent: 126. Origin: Steve's folder review continuing after the 126 program closed.
- Related but NOT here: `ganda kanban done` crash when parent already done (TimeWarp.Zana bug —
  belongs in the ganda/timewarp-flow backlog, not this repo); `dotnet fixie` intermittent
  `_Fixie_GetTargetFrameworks` failure on aspire-tests (tooling, tracked in 126-009 Results).


## Implementation Plan (2026-07-27)

### DotSettings
Skimmed both files: only ReSharper `NamespaceFoldersToSkip` for obsolete feature folder paths
(todo-items, old SPA application/actions). No real convention → delete both.

### #nullable enable classification
**Remove (hand-written file-level, 11 files):**
- tests/common/timewarp-testing: scoped-sender, testing-convention, test-server-application,
  web-api-test-service, web-application-host
- web-server program.cs, sample-environment-check.cs
- platform/postgres: environment-check, startup-hosted-service
- web-spa: base-component.cs, account-state.cs

**Carve-out (leave):**
- Generator emitters (typed-id, mock-registry, ingress-prefix, contracts-mixin) — directive in emitted strings
- feature-filename-grammar.g.cs — generated
- Analyzer test FluentValidation stubs / source strings — fixture input, not product

### Gates
dev build 0/0, dev test, template-smoke both matrices

## Session

- Created: 2026-07-27 — filed from maintainer review findings.
- Orchestrator / implement / review: grok-build 2026-07-27

## Results

### What was implemented
1. Deleted both ReSharper `.DotSettings` files (NamespaceFoldersToSkip only for obsolete feature paths — no real conventions to migrate).
2. Removed file-level `#nullable enable` from 11 hand-written sources (5 testing helpers, program.cs, sample env check, 2 postgres platform files, 2 SPA files). Collapsed IDE2000 multi-blank-line on program.cs.
3. Carve-outs preserved: generator emitted strings, `feature-filename-grammar.g.cs`, analyzer test FluentValidation/source stubs.

### Files changed
- Deleted: `web-spa.csproj.DotSettings`, `web-contracts.csproj.DotSettings`
- Stripped `#nullable enable` from 11 `.cs` files listed in Implementation Plan

### Test outcomes
| Gate | Result |
|------|--------|
| `dev build` | 0/0 |
| `dev test` | All passed |
| `dev template-smoke` | Both matrices OK; no DotSettings in generated apps |

### Review
- Rounds: 1; effort 1 general
- Final: 0 open
- Disposition: clean
