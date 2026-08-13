# Lift SSH.NET to 2026.0.0 to clear NU1903

## Description

`Testcontainers.PostgreSql` 4.13.0 pulls SSH.NET 2025.1.0. GitHub advisory
GHSA-q939-rpr3-3284 (SCP recursive download path traversal) is now in the
NuGet audit database, so restore treats NU1903 as an error and fails
`web-infrastructure-tests` (and template-smoke SmokeDefault). Lift the
transitive package to patched 2026.0.0 — same pattern as Microsoft.OpenApi
on foundation-server. Do not suppress NU1903.

## Checklist

- [x] Add CPM pin `SSH.NET` 2026.0.0
- [x] Direct `PackageReference` on `web-infrastructure-tests` (only Testcontainers consumer)
- [x] Restore `web-infrastructure-tests` locally — no NU1903
- [ ] Push and confirm PR #301 `ci` + `template-smoke` green

## Notes

- Advisory published after the last green #301 run (b039d4a9). Not caused by the razor/@code work.
- Testcontainers 4.13.0 is still latest; no upstream bump available.

## Session

- Implementation: grok 2026-08-13 (PR #301 CI unblock)
