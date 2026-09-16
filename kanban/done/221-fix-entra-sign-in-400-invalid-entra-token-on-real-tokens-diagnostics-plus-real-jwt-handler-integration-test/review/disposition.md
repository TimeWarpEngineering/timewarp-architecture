# Disposition — task 221

**Date:** 2026-09-16
**Outcome:** clean
**Rounds:** 1
**Final open count:** 0

## Summary

Effort-1 general review of branch `task/221-fix-entra-sign-in-400-invalid-entra-token-on-real` vs `origin/master`. Round 1 (`general`) raised no issues. Root-cause story re-verified against ASP.NET Core 10.0.11: default `ClaimActions.DeleteClaim("iss")` runs after `OnTokenValidated` on empty JSON when user-info is off; `RemoteAuthenticationHandler` clears `Properties.RedirectUri` before `OnTicketReceived`, so the Items stash is required. Diagnostics, named 400 details, boot version stamp, and the real-JWT handler round-trip test match the brief. Disposition is **clean**.

## Exception log (if accepted-exceptions)

(none)

## Escalations

- None
