# Round 1 — merged findings
**Date:** 2026-10-06
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None raised. The orchestrator spot-checked the general reviewer's result:
`get-credentials-handler-application.cs` builds the flags from active credentials through
`CredentialRules` (revoked rows get false). The SPA reads `CanRevoke`, `CanRename` and
`CanLinkMicrosoft365` from `CredentialsState`. `git grep -i offer` over source, tests and skills
finds only the unrelated Entra "sign-in offered" concept and the retired-ID SSOT comment.

## Duplicates / conflicts

- None.
