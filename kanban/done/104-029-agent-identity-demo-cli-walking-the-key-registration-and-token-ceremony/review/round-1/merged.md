# Round 1 — merged findings
**Date:** 2026-07-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/agent-identity-cli/services/agent-wire-dtos.cs
- Description: WhoAmIResponse Kind/TrustTier as string; server emits numeric enums → STJ throws after HTTP 200.
- Suggestion: Use PrincipalKind / TrustTier; add offline deserialize fixture test.
- Source: general
- Disposition notes: Fixed 2026-07-20 — enums on DTO + whoami-wire-tests.cs numeric fixture.

### M2 — Severity: nit — Status: fixed
- File: tools/agent-identity-cli/endpoints/token-command.cs
- Description: Double PEM load when store lacks KeyId.
- Suggestion: Load once and reuse.
- Source: general
- Disposition notes: Fixed 2026-07-20 — single LoadKey.

## Duplicates / conflicts

None.
