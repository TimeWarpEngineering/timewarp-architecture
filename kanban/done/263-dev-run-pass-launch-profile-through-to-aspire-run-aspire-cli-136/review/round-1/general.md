# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** branch vs master: `tools/dev-cli/endpoints/run-command.cs`, `tools/dev-cli/services/aspire-run.cs`, `tests/tools/dev-cli-tests/aspire-run-tests.cs`, `dev-cli-tests.csproj`, AGENTS.md.

## Summary

Adds `--launch-profile`/`-lp` to `dev run`, forwarded to `aspire run`. Logic is extracted into a pure
`AspireRun` helper (picked up by `services/**/*.cs` in dev-cli's Directory.Build.props and
Compile-included by dev-cli-tests). No-option path is byte-identical (`run --apphost <csproj>`) and
skips the version probe; with the option, the profile is validated against the real
launchSettings.json (ordinal, lists names) and a < 13.6 / unparseable CLI fails with the update
command. `Version(13,6)` vs parsed `13.6.0` compares correctly (13.6.0 > 13.6). Design/Purpose
regions reconciled, env-precedence recorded as verified. Re-verified: `--filter-class AspireRun`
12/12 pass; `ganda repo audit` passes. Low risk.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/endpoints/run-command.cs:29
- Description: Route description still says "(Development environment)"; with `-lp` the profile's
  `environmentVariables` win (as the Design region records), so the label is only true for profiles
  that set Development.
- Suggestion: Optionally reword; both declared profiles (https, http) set Development today.
- Status: open
