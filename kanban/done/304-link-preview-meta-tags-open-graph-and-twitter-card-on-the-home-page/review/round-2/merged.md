# Round 2 — merged findings
**Date:** 2026-10-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/mail/http-app-base-url-accessor-server.cs
- Description: The forwarded-origin policy was duplicated. It is now the shared `GetRequestOrigin`.
- Source: general (round 1)

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/configuration/link-preview.cs
- Description: An unresolvable origin threw during render. Tags are now omitted instead.
- Source: general (round 1)

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/components/LinkPreviewMeta.razor
- Description: `Resolve` ran once per tag. It now runs once in `OnInitialized`.
- Source: general (round 1)

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-server/configuration/link-preview.cs
- Description: The folder and namespace disagreed. Both files moved to `configuration/`.
- Source: general (round 1)

No new findings.

## Duplicates / conflicts

- None.
