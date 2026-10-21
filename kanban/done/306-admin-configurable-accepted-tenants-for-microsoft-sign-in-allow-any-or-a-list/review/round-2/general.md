# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta since round 1, `9896f0a67..e00a0e7dd` (8 files, +114/−26). Re-verified M1–M3 against the post-fix tree.

## Summary

The fix commit seeds the challenge fixture with `entraAcceptedTenantIds: [TrustedTenantId]` and adds an Any-mode bootstrap case for `UntrustedTenantId` that restores the row in `finally`. The settings validator now calls `EntraTenantIds.TryNormalize` (one source for the empty-Guid, duplicate-collapse, and distinct-count rules), with two new tests for the raw vs distinct cap. The admin help text now says `common (or consumers)`. Boyscout: the two runfiles drop their NU1510 framework `#:package` refs, and `ganda repo audit --fix` removed the now-orphaned `Microsoft.Extensions.Logging.Abstractions` pin. No `#:package` or `PackageReference` still names it. `Microsoft.Extensions.Options` stays pinned for `effective-roles-resolver-tests.cs`, and the updated props comment says so.

The delta still matches the goal restatement and the original task. No new defects.

Gates re-run by this reviewer:

- `dotnet run tools/dev-cli/dev.cs -- build`: 0 warnings, 0 errors
- update-site-settings runfile 30/30 (28 + 2 new cap tests)
- get-entra-sign-in-offered runfile 5/5; seed-on-read-site-settings-store runfile 11/11 (both build with no direct Options / Logging.Abstractions refs)
- web-server-integration-tests: 350 total, 349 passed, 0 failed, 1 skipped (`RunForever`, intentionally skipped)

## Prior findings

| ID | Severity | Status | Verification |
|----|----------|--------|--------------|
| M1 | bug | fixed | `entra-challenge-tests.cs:168` seeds `[TrustedTenantId]`. The suite is green (was 10 failed). The new `Bootstrap_Any_Mode_Untrusted_Tenant_Should_Redirect_To_Choose` covers Any mode over HTTP. |
| M2 | suggestion | fixed | `site-settings-details-contracts.cs:45-56` delegates to `EntraTenantIds.TryNormalize`. The Design region was updated. Tests prove 33 copies of one id pass and 33 distinct ids fail with the shared message. |
| M3 | nit | fixed | `AuthenticationPage.razor:188` says "common (or consumers)". |

## Issues

None.
