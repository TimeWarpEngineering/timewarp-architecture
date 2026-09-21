# Round 2 — general
**Date:** 2026-09-21
**Scope reviewed:** post-fix delta for M1 (template-install wipe + TemplateNupkg.FindArchitectureTemplateNupkg + tests)

## Summary

M1 is fixed. `template-install` now deletes `artifacts/template-install/` before packing (same pattern as `template-smoke`), and selection moved to `TemplateNupkg.FindArchitectureTemplateNupkg`, which orders by `LastWriteTimeUtc` then ordinal path. Unit tests cover a missing directory and the beta.9 (older mtime) vs beta.10 (newer mtime) leftover case with an Analyzers distractor; `dotnet test -c Release` in `tests/tools/dev-cli-tests` passed 68/68. No new defects on the fix delta.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/template-install-command.cs:75-80; tools/dev-cli/services/template-nupkg.cs:38-50; tests/tools/dev-cli-tests/template-nupkg-tests.cs:32-69
- Description: Re-verified against `f45196c1` and current tree. Wipe runs on the non-dry-run path before `Directory.CreateDirectory(PackagesDir)` / pack. Former private `OrderByDescending(path, Ordinal)` helper is gone; shared `FindArchitectureTemplateNupkg` uses `File.GetLastWriteTimeUtc` with ordinal only as a tie-breaker. `NewerPack_Should_WinOverLexicographicallyLaterStaleVersion` writes beta.9 with an older UTC mtime and beta.10 with a newer one (plus Analyzers filtered out), asserting beta.10 wins — the exact failure mode where ordinal sort would have preferred beta.9. Dry-run (`./bin/dev template-install --dry-run`) still only prints intended paths and does not wipe.
- Status: fixed
