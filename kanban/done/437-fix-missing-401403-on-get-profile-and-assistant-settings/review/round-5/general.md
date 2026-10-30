# Round 5 — general
**Date:** 2026-10-29
**Scope reviewed:** delta since round 4 (base `0c6ffb48`, HEAD `75b50b45`). The only product change is `source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs` (5 lines). The rest of the delta is kitchen files.

## Summary

implement-fix-4 rewrote the GetProfile `[EndpointAllowAnonymous]` reason as spec-reader prose:
"This read is dual-mode: an anonymous caller gets the demo profile, and a signed-in caller gets
their stored profile." The task ids are gone from the published reason. The Design region keeps
the history and now says why the reason has no task ids. The test `Profile_Get_Should_State_Anonymous_Intent`
asserts the `dual-mode` fragment, and the new text still contains it. I re-ran
`dotnet test -c Release -- --filter-class OpenApiAnonymousIntent_` in
`tests/container-apps/web/web-server-integration-tests`, and 6 of 6 passed. A grep of every
`[EndpointAllowAnonymous(...)]` reason under `source/` found no other task ids. M1 is fixed, and
this delta has no new findings.

The implementer restatement matches the original task: keep both GETs anonymous on purpose, state
that on the operation, audit the other GETs, and add 401/403 only where the handler returns them.
That is not a misread.

## Issues

<!-- None. M1 (carried) verified fixed. -->
