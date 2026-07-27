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

- [ ] Skim + delete both `.DotSettings` files (surface any real-convention setting found)
- [ ] Classify all 18 `#nullable enable` hits: hand-written top-level vs emitter-string vs
      `.g.cs`; remove only the first category
- [ ] `dev build` 0/0 (nullable warnings-as-errors will catch any file that actually depended
      on a local directive), `dev test`
- [ ] `dev template-smoke` both matrices (template content changed — file deletions)

## Notes

- Parent: 126. Origin: Steve's folder review continuing after the 126 program closed.
- Related but NOT here: `ganda kanban done` crash when parent already done (TimeWarp.Zana bug —
  belongs in the ganda/timewarp-flow backlog, not this repo); `dotnet fixie` intermittent
  `_Fixie_GetTargetFrameworks` failure on aspire-tests (tooling, tracked in 126-009 Results).

## Session

- Created: 2026-07-27 — filed from maintainer review findings.
