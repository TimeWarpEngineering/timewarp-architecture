# Round 2 — general
**Date:** 2026-09-21
**Scope reviewed:** post-fix delta on .github/workflows/workflow.yml; re-verify M1/M2

## Summary

Re-verified the `repo-audit` Install ganda step after the M1 fix. YAML `run: |` indent strip leaves a valid script: unquoted `<<EOF` heredoc, `EOF` at column 0, `${NUGET_USER}` / `${GITHUB_TOKEN}` expand into the config, nuget.org is absent from `packageSources` (`<clear />` + github only), and `PATH` is exported in-step before `ganda --version`. Bash `case` globs refuse beta.0–9, beta.10–15 (and beta.15+metadata) and correctly pass `1.0.0-beta.29` / `1.0.0-beta.29+hash`. Github-only install is intentional fail-closed if Packages auth or restore cannot supply a current tool; no new defects on the fix delta. M2 wontfix still holds.

## Prior findings

### M1 — Severity: suggestion — Status: fixed
- File: .github/workflows/workflow.yml
- Description: re-verify the github-only install + version gate actually fail-closes
- Status: fixed — confirmed: github-only configfile; post-install version gate; same-step PATH export; heredoc/YAML valid; globs match beta.15 / beta.9 / beta.10 and do not match beta.29+hash

### M2 — Severity: suggestion — Status: wontfix
- File: .github/workflows/workflow.yml path filters
- Description: confirm wontfix still holds (kanban/** out of CI by design)
- Status: wontfix — agree; task CI guard is for co-located `*-tests.cs` under `source/**` (already filtered); adding `kanban/**` would start this workflow on every kanban-only PR

## Issues
