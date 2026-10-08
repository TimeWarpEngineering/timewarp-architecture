# Round 1 — merged findings
**Date:** 2026-10-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 2 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/Directory.Build.props:13
- Description: Project NoWarn adds IDE0211 plus ten IDE ids; broad against the "no blanket suppressions" rule.
- Suggestion: Scope via an .editorconfig section for the Nuru content files.
- Source: general
- Disposition notes: The suppression is scoped to the `tools/dev-cli` project only, not repo-wide, and the Design comment documents each id. The offending files are TimeWarp.Nuru.DevCli content files in the NuGet cache, outside the repo tree. `.editorconfig` sections cannot match them, and a global analyzer config would need absolute machine paths. Fixing the style upstream in Nuru is the real remedy; not on this task. (orchestrator)

### M2 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/features/identity/site-settings-seed-hosted-service-server.cs:41-42
- Description: Seed retry budget 30 → 180 attempts (quiet 5 → 120); a deployment with failed migrations now waits 3 min before failing.
- Suggestion: Keep a short production budget or make it configurable.
- Source: general
- Disposition notes: Still bounded and fail-loud: the last attempt skips the probe and surfaces the real 42P01. Each attempt is a cheap catalog probe. The Design region records why 180s is needed (the EF 11 tool runs `--verbose` under Aspire, so migrations take about 90s on first boot). A slower failure on a broken deployment beats a crashed host on every clean first run. An environment-split budget would be a second code path with no driver today. (orchestrator)

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj:24-26
- Description: Stale comment describes a Microsoft.Extensions.Hosting 10.0.9 PackageReference that no longer exists.
- Suggestion: Remove the comment.
- Source: general
- Disposition notes: Comment removed (it was already stale on master; boyscout).

### M4 — Severity: nit — Status: wontfix
- File: timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj:1
- Description: File went CRLF → LF, so the whole file shows in the diff.
- Suggestion: Review with `-w`.
- Source: general
- Disposition notes: The repo `.gitattributes` sets `* text=auto eol=lf`, so LF is the correct normalized form. The boyscout normalization is welcome; `git diff -w` shows the real one-line TFM change. (orchestrator)

## Duplicates / conflicts

- None (single reviewer).
