# Round 1 — merged findings
**Date:** 2026-07-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `timewarp-architecture.slnx`
- Description: Platform projects deleted rather than `#if (false)` dual-use; monorepo loses solution membership.
- Suggestion: Restore platform Project lines under `<!--#if (false) -->` so monorepo keeps membership while generate always strips them. Do not reintroduce `*Packages` symbols.
- Source: general
- Disposition notes: Restored foundation/libraries/analyzers + matching test projects under `<!--#if (false) -->` … `<!--#endif -->` with monorepo-only comment. XML comments keep membership for monorepo; template engine always strips on generate. No `*Packages` symbols.

### M2 — Severity: nit — Status: fixed
- File: `tools/dev-cli` / `bin/dev`
- Description: Stale AOT `./bin/dev` misses new smoke gates until re-self-install; CI uses runfile.
- Suggestion: Prefer runfile or self-install after change; optional Design note.
- Source: general
- Disposition notes: Documented in TemplateSmokeCommand Design region: prefer `dotnet run tools/dev-cli/dev.cs -- template-smoke` or re-self-install; CI uses runfile. Not a product defect.

### M3 — Severity: suggestion — Status: fixed
- File: `template-smoke-command.cs` scan roots
- Description: Pre-scan omits packed `source/Directory.Build.props` and `tests/Directory.Build.props`.
- Suggestion: Add both to `SourceNameLiteralScanRelativeFiles`.
- Source: general
- Disposition notes: Added both files to `SourceNameLiteralScanRelativeFiles`.

## Duplicates / conflicts

- None
