# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tools/dev-cli/services/skill-spec-lint.cs:106
- Description: `IsOnPath` probes `vally` and `vally.exe`. A Windows `npm install -g` creates `vally.cmd`, so on native Windows an installed vally reads as "not on PATH". The launch through `Shell.Builder("vally")` would also need PATHEXT resolution.
- Suggestion: Resolve PATHEXT in both the probe and the launch, or drop the `.exe` probe.
- Source: general
- Disposition notes: wontfix (review orchestrator, 2026-10-22). Nit severity, so it is optional. The gate it replaces is a Linux CI job, and the documented pre-PR path is Linux/WSL. Windows support needs work in both the probe and the launch, which is beyond this task's goal of matching CI. The failure is safe: it refuses and prints the install, so it never passes silently. A future Windows dev-cli pass can pick this up.

## Duplicates / conflicts

- None (single reviewer).
