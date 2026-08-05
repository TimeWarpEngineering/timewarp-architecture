# Round 1 — general
**Date:** 2026-08-05
**Scope reviewed:** commit 6bd81f13 vs c8ae9def

## Summary

The change swaps `GetProfile.Handler` from foundation's `ICurrentUserService` (which reads a
`"UserId"` claim that no auth scheme in this repo ever emits) to the platform's
`ICurrentPrincipalAccessor` (which reads the `timewarp:principal_id` claim identity-session
issuance actually sets), fixing authenticated `GetProfile` calls silently falling through to the
anonymous contract mock. The handler diff mirrors the existing `get-credentials-handler-
application.cs` precedent exactly (null-principal → 401/mock, `PrincipalId.Value` → `Guid`), the
co-located Jaribu stub swap is a faithful 1:1 rename with one added determinism assertion, and the
new in-proc integration test is closely modeled on `passkey-authentication-tests.cs` (same
register→authenticate→isolated-cookie pattern, same C-create SetupOnce/CleanUpOnce fixture
lifetime). Design/Purpose regions in all three touched product files are reconciled to the new
implementation — no stale `ICurrentUserService` wording remains. Risk is low: the change is narrowly
scoped, has a real regression test that fails without the fix (per the implementer's Notes), and a
repo-wide grep confirms `ICurrentUserService` now has zero consumers outside `foundation-
infrastructure` itself, so no other web-family handler carries the same latent bug.

## Gate verification

- **`dev build`** — claimed 0 warnings / 0 errors. Ran `./bin/dev build`: **Build succeeded, 0
  Warning(s), 0 Error(s).** PASS.
- **`dotnet run source/container-apps/web/features/profile/get-profile/get-profile-tests.cs`** —
  claimed 10/10. Ran it: **Total 10, Passed 10** (3 contract, 3 store, 4 handler). PASS.
- **`cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release`** —
  claimed 112 total / 16 failed / 95 passed / 1 skipped, all 16 failures pre-existing in agent-key/
  credential features, both new `GetProfileSession` tests passing. Ran it: **112 total, 16 failed,
  95 succeeded, 1 skipped** — exact match. The 16 failing test names are all in credential/agent-key
  surfaces (`ValidationError_Given_Empty_Name`, `ValidationError_Given_Empty_UserId`,
  `Conflict_Given_Duplicate_Key`, `ValidationError_Given_Oversized_PublicKey`,
  `ValidationError_Given_Oversized_Label`, `Forbidden_Given_Quarantined_Principal`,
  `Ok_With_Two_Active_Credentials_Given_AddAgentKey_On_Bearer_Principal`,
  `Ok_With_One_Active_Key_Given_Rotation_Adds_New_Then_Revokes_Old`,
  `Conflict_Given_Passkey_Handle_Already_Owned_By_Another_Principal`,
  `Conflict_Given_AgentKey_Handle_Already_Owned_By_Another_Principal`,
  `Never_Serializes_Handle_Or_PublicMaterial`, `NotFound_Given_Another_Principals_Credential`,
  `NotFound_Given_Unknown_CredentialId`, `Conflict_Given_Last_Active_Credential`,
  `Conflict_Given_Already_Revoked_Credential`, `Conflict_Given_Duplicate_Credential`) — none touch
  profile features. Re-ran the new suite in isolation with `-- --filter-class GetProfileSession`:
  **total 2, passed 2** (`RealProfile_Given_Authorized_Session`, `AnonymousMock_Given_No_Session`).
  PASS.

## Issues

No issues found. Zero findings — the fix is a faithful application of the existing
`ICurrentPrincipalAccessor` precedent, the regions are reconciled, the co-located test stub swap
preserves all prior coverage plus a determinism assertion, the new integration test genuinely pins
the regression (authenticated path asserts `Alias == "Member"` and `!= "alias"`) while keeping a
truly anonymous control case (fresh `HttpClient` with no cookie), the fixture lifetime follows the
C-create default per AGENTS.md/145-008, and all three gate claims verified exactly against a live
run. A repo-wide grep found no other web-family consumer of the now-orphaned `ICurrentUserService`,
so there is no in-scope follow-up finding there; the implementer's plan already flags leaving
`CurrentUserService` registered in foundation as an explicit, deliberate non-goal for this task.
