# Round 2 — merged findings
**Date:** 2026-10-07
**Sources:** general (re-review of e1528cc83..5e759c6b7)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 1 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: wontfix
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs (aca web hop)
- Description: The ACA web hop is HTTPS to `web-server.internal.<domain>` carrying the public Host, so web routes are expected to fail through the ACA ingress.
- Disposition notes: Docs are fixed in 5e759c6b7. The inline comment, the AppHost Open Questions entry, the tw-deploy "expect" bullet and task.md Results now all say that web routes are expected to fail on aca until the maintainer decides. The routing change itself is an architecture and security-design decision for the maintainer: an aca-only route set without original-Host, with web-server reading X-Forwarded-Host for RP-ID selection. Verifying it also needs a real deploy, which workers are forbidden to do. The other targets are unaffected. Decided by: review orchestrator, escalated to maintainer.

### M2 — Severity: suggestion — Status: fixed
- Disposition notes: 5e759c6b7. The docs are accurate. The `Publish_Should_PinThePostgresSecrets` fact pins the secrets.

### M3 — Severity: suggestion — Status: fixed
- Disposition notes: 5e759c6b7. The wording is accurate. The `Publish_Should_PinTheFlexibleServerFirewall` fact pins the firewall rules.

### M4 — Severity: suggestion — Status: fixed
- Disposition notes: 5e759c6b7. The suite now slices the ingress block and reads `external`. A missing `external` field fails.

### M5 — Severity: suggestion — Status: fixed
- Disposition notes: 5e759c6b7. The subscription precedence is: env var, then the AppHost user secret (`dotnet user-secrets list` via Amuru), then az. Only the az fallback is injected. Covered by tests.

### M6 — Severity: suggestion — Status: fixed
- Disposition notes: 5e759c6b7. The migration recipe now names the deployment-state keys and the Key Vault route.

### M7 — Severity: nit — Status: fixed
- Disposition notes: 5e759c6b7.

### N1 — Severity: nit — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs (firewall fact)
- Description: The firewall fact parsed only top-level rule declarations. A `[for]` or nested rule could slip past it.
- Disposition notes: Fixed by the review orchestrator. A raw occurrence count of `firewallRules` and `startIpAddress` across all modules must equal 1. Verified: the clean output passes 9/9, and a looped 0.0.0.0–255.255.255.255 rule fails the fact.

## Duplicates / conflicts

- None.
