# Round 1 — merged findings
**Date:** 2026-09-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor.cs:20
- Description: Design region still described the card title as Credential.Label and the subtitle as "Microsoft 365" — both removed by this diff.
- Suggestion: Reconcile the Design region.
- Source: general
- Disposition notes: Design region rewritten (no subtitle; Entra row title = provider Label, context = AccountHint).

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs:197
- Description: Settings Entra page tests seeded the email as Label (pre-250 shape), so no page-level test covered the new shape.
- Suggestion: Seed Label "Microsoft 365" + AccountHint email.
- Source: general
- Disposition notes: AddEntraAccountAsync now seeds label "Microsoft 365" and accountHint; both tests assert the email and ">Microsoft 365<"; web-server-integration-tests 255/255 pass.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/platform/postgres/migrations/20260923180147_AddCredentialAccountHint.cs:36
- Description: Down cannot restore name-claim Labels dropped by Up; undocumented.
- Suggestion: Comment the lossy rollback.
- Source: general
- Disposition notes: Comment added in Down.

### M4 — Severity: nit — Status: fixed
- File: tests/.../identity-contracts-serialization-tests.cs, entra-challenge-tests.cs, credential-list-render-tests.cs, credential-row-presenter-tests.cs
- Description: Diff added UTF-8 BOMs against .editorconfig `charset = utf-8`.
- Suggestion: Strip BOMs.
- Source: general
- Disposition notes: BOMs stripped (EF-generated migration files left as generated).

### M5 — Severity: nit — Status: fixed
- File: source/libraries/timewarp-identity/credentials/credential.cs:39; credential-row-presenter-tests.cs Purpose
- Description: Over-long comment lines.
- Suggestion: Re-wrap.
- Source: general
- Disposition notes: Re-wrapped.

## Duplicates / conflicts

- None.
