# Round 2 — merged findings
**Date:** 2026-09-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/template-install-command.cs:75-80; tools/dev-cli/services/template-nupkg.cs:38-50; tests/tools/dev-cli-tests/template-nupkg-tests.cs:32-69
- Description: `template-install` accumulated packs and picked "newest" by ordinal path, so leftover `beta.9` could win over `beta.10`.
- Suggestion: Wipe pack dir; select by LastWriteTimeUtc; unit-test leftover vs newer.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. Wipe before pack; `TemplateNupkg.FindArchitectureTemplateNupkg` uses LastWriteTimeUtc; tests prove beta.10 wins over lexicographically later beta.9. No new findings.

## Duplicates / conflicts

- None. Prior M1 carried forward as fixed. No new IDs.
