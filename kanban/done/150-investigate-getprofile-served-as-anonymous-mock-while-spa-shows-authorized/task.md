# Investigate GetProfile served as anonymous mock while SPA shows authorized

## Description

Observed 2026-08-05 in a Dev Aspire run: the SPA's `AuthorizeView` renders the **Authorized**
profile menu (Profile / Settings / Sign out), but the Profile page and header show the
**contract mock** profile — display name "alias", grey-rect avatar. That mock is only returned
by `GetProfile.Handler` when `CurrentUserService.UserId` is null
(`source/container-apps/web/features/profile/get-profile/get-profile-handler-application.cs:51`),
meaning the server handled the GetProfile API call as **anonymous** even though the client
believes it is signed in.

So either (a) the API call is not carrying the auth cookie/token, (b) the server auth pipeline
is not populating `ICurrentUserService.UserId` from the principal it does receive, or (c) the
SPA's authentication state provider claims authorized when the backend session does not exist
(e.g. mock client auth state without mock server auth).

Recent commit `55ee9384` "fix(aspire): stop forcing Authentication:UseMock for every Dev run"
changed the auth posture of Dev runs and is the prime suspect for exposing (or causing) the
mismatch — before it, mock auth likely made both client and server agree.

## Requirements

- Root-cause the client-authorized / server-anonymous split; state which of (a)/(b)/(c) it is
  with evidence (request headers, server logs, auth state provider registration).
- Decide and implement the correct posture for a plain `dev run` (no UseMock): either the SPA
  must not render Authorized state without a real backend session, or the real session must
  flow to `ICurrentUserService` on API calls.
- Regression coverage: a test (co-located Jaribu or aspire-tests lane, whichever fits the
  seam) that fails if an authorized SPA session receives the anonymous mock profile.

## Checklist

- [x] Root cause established by code trace (in lieu of live reproduce — the claim mismatch is
      provable statically; the regression test reproduces it at the host seam)
- [x] Determine what `AuthenticationStateProvider` implementation is active in the SPA for
      this run mode and why it reports authorized (real `IdentitySessionAuthenticationStateProvider`;
      authorized because the backend session genuinely exists)
- [x] Determine what populates `ICurrentUserService.UserId` server-side and why it was null
      for this request (reads a `"UserId"` claim no scheme emits — see plan)
- [x] Assess `55ee9384` (UseMock no longer forced in Dev) for its role; document intended Dev
      auth posture (exposed the bug, did not cause it; posture documented in plan)
- [x] Implement the fix on the correct side (server: handler swapped to `ICurrentPrincipalAccessor`,
      commit `6bd81f13`)
- [x] Add regression test for the authorized-SPA-gets-mock-profile failure mode
      (`get-profile-session-tests.cs`, proven to fail pre-fix)
- [x] `dev build` 0/0; affected test suites green (16 pre-existing unrelated integration
      failures noted — see Results)
- [x] Results with How to validate

## Notes

- The visible symptom (grey avatar, "alias" name) is cosmetic fallout; the cosmetic side is
  task 149. This task owns the auth mismatch only.
- Mock-auth machinery reference: task 145-009 (fail-closed `Authentication:UseMock`,
  `X-TimeWarp-Mock-Principal-Id`, TWA0021).

### Implementation plan (Phase 2, 2026-08-05)

**Root cause — hypothesis (b), claim-type mismatch.** The identity-session cookie principal
carries exactly one id claim, `timewarp:principal_id`
(`platform/identity-host/cookie-browser-session-service-server.cs:39`,
`identity-session-defaults-server.cs:23`). The sole `ICurrentUserService` implementation,
foundation's `CurrentUserService`
(`source/foundation/foundation-infrastructure/services/current-user-service.cs:21`), looks for a
claim literally named `"UserId"` — which identity-session issuance never emits (it was designed
for the non-default Entra token path). So `HttpContext.User` is authenticated but
`CurrentUserService.UserId` is null → GetProfile returns the contract mock.

Ruled out with evidence: (a) cookie IS sent (the SPA's Authorized state itself comes from
`GET api/identity/session` over the same client/cookie); (c) SPA auth state is genuine
(`UseMock=false` everywhere; real `IdentitySessionAuthenticationStateProvider` registered);
client mock mode is off (`MOCK_WEB_API` define commented out in `web-spa.csproj`). The 145-009
mock header path also emits `timewarp:principal_id`, never `"UserId"` — same mismatch.

`55ee9384` **exposed** the bug, did not cause it: GetProfile's authenticated branch was never
reachable end-to-end under passkey auth; task 148 tests stubbed `ICurrentUserService` directly.

**Intended Dev auth posture:** plain `dev run` = passkey-first real auth, no mock on either
side; mock auth is opt-in AppHost config (`--Authentication:UseMock=true`), fail-closed outside
Development/Testing. The SPA rendering Authorized was CORRECT — fix belongs server-side.

**Fix steps:**
1. `get-profile-handler-application.cs`: replace `ICurrentUserService` with the existing
   scheme-agnostic `ICurrentPrincipalAccessor`
   (`platform/identity-host/i-current-principal-accessor-application.cs`; precedent:
   `get-credentials-handler-application.cs`). Null principal → mock (D3 unchanged); otherwise
   `ProfileId.From(principalId.Value.Value)` + existing create-if-missing flow. Reconcile
   Purpose/Design regions.
2. `get-profile-contracts.cs`: fix the Design-region sentence referencing
   `ICurrentUserService.UserId`.
3. `get-profile-tests.cs`: `StubCurrentUserService` → `StubCurrentPrincipalAccessor`; keep all
   scenarios; add determinism assertion (same Guid → row keyed `ProfileId.From(guid)`).
4. New regression test in `tests/container-apps/web/web-server-integration-tests/features/profile/`
   `get-profile-session-tests.cs` (in-proc lane — the bug lives at the host seam; modeled on
   `features/identity/passkey-authentication-tests.cs`): (i) passkey register + authenticate,
   cookie-bearing GET of the GetProfile route asserts `Alias == "Member"` and `!= "alias"`;
   (ii) anonymous request still gets the demo mock (`Alias == "alias"`, guards D3).
5. NOT changed: foundation `CurrentUserService` (left registered; consumer-less in web — flag
   optional cleanup follow-up), SPA auth registrations, AppHost config, endpoint auth posture.

**Alternatives rejected:** claims transformation adding `"UserId"` (magic string, every scheme
must remember it); teaching foundation about `timewarp:principal_id` (wrong dependency
direction); making the SPA render NotAuthorized (SPA is right; would break passkey dogfood).

## Results

**Root cause (hypothesis b — claim-type mismatch):** identity-session cookies issue exactly one
id claim, `timewarp:principal_id` (`cookie-browser-session-service-server.cs`,
`identity-session-defaults-server.cs`). Foundation's `CurrentUserService` — the sole
`ICurrentUserService` implementation — reads a claim literally named `"UserId"`
(`current-user-service.cs:21`) that no scheme emits (it targeted the non-default Entra token
path). So `HttpContext.User` was authenticated but `UserId` was null and `GetProfile.Handler`
returned the anonymous contract mock. Ruled out: (a) cookie not sent — the SPA's Authorized
state comes from `GET api/identity/session` over the same client/cookie; (c) false SPA auth —
`UseMock=false` everywhere, real `IdentitySessionAuthenticationStateProvider` registered;
client mock mode — `MOCK_WEB_API` define is off. `55ee9384` exposed the bug (made real passkey
sessions the Dev default), did not cause it; GetProfile's authenticated branch was never
reachable end-to-end before, because task 148's tests stubbed `ICurrentUserService` directly.

**Intended Dev auth posture (documented):** plain `dev run` = passkey-first real auth, no mock
on either side; mock auth is opt-in AppHost config (`--Authentication:UseMock=true`),
fail-closed outside Development/Testing. The SPA rendering Authorized was correct — the fix
belongs server-side.

**Fix (commit `6bd81f13`, merged to dev):**
- `get-profile-handler-application.cs` — handler ctor swapped `ICurrentUserService` →
  `ICurrentPrincipalAccessor` (scheme-agnostic; same claim all three schemes emit; precedent
  `get-credentials-handler-application.cs`); null principal → mock (D3 demo path unchanged);
  otherwise `PrincipalId.Value` Guid feeds `ProfileId.From(...)` and `GetAvatarDataUri(...)`.
  Purpose/Design regions reconciled.
- `get-profile-contracts.cs` — Design region wording updated (no behavior change).
- `get-profile-tests.cs` — `StubCurrentUserService` → `StubCurrentPrincipalAccessor`; all four
  handler scenarios preserved; added store-row keying assertion.
- NEW `tests/container-apps/web/web-server-integration-tests/features/profile/get-profile-session-tests.cs`
  (in-proc lane, modeled on `passkey-authentication-tests.cs`): authorized passkey session gets
  `Alias == "Member"` (not `"alias"`) + real avatar data URI; anonymous request still gets the
  demo mock. Proven to catch the bug: with the handler change stashed, the authorized test
  fails with `profileResponse.Alias should be "Member" but was "alias"`.
- Deliberately NOT changed: foundation `CurrentUserService` + registration (now consumer-less
  in the web family — optional cleanup candidate), SPA auth registrations, AppHost config,
  endpoint auth markers.

**Gates (implementer, independently re-verified by reviewer):** `dev build` 0/0; co-located
runfile `dotnet run .../get-profile-tests.cs` 10/10; integration suite 112 total / 16 failed /
95 passed / 1 skipped — the 16 failures are pre-existing at base `c8ae9def` (identical set
observed with all task-150 changes stashed) and confined to agent-key/credential features;
`GetProfileSession` class 2/2 green in isolated re-run.

**Review (Phase 4b):** 1 round, effort 1 (single general reviewer), 0 findings raised
(0 bug / 0 suggestion / 0 nit). Disposition: **clean**. Artifacts:
`review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
`review/disposition.md`.

**Known issue out of scope:** 16 pre-existing `web-server-integration-tests` failures in
agent-key/credential features exist at dev tip independent of this task — needs its own task.

### How to validate

**Smoke (UI):**
1. `dev run`, open the web app, register/sign in with a passkey.
2. Open the Profile page (avatar menu → Profile).
3. Expect: display name **Member** (not `alias`) and a real generated multiavatar image (not a
   grey placeholder). Header avatar shows the same image.
4. Sign out → profile data reverts to the anonymous demo mock (`alias`).

**Automated gates:**
```bash
dev build                                                              # expect 0 warnings / 0 errors
dotnet run source/container-apps/web/features/profile/get-profile/get-profile-tests.cs   # expect 10/10
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class GetProfileSession  # expect 2/2
```

**Expect:** `RealProfile_Given_Authorized_Session` and `AnonymousMock_Given_No_Session` both
pass. To re-prove the regression coverage: revert only
`get-profile-handler-application.cs` to `c8ae9def` and the authorized test fails with
`Alias should be "Member" but was "alias"`.

**Not in scope:** the 16 pre-existing agent-key/credential integration failures (separate
task); removal of the now consumer-less foundation `CurrentUserService`.

## Session

- Created: Claude (2026-08-05)
- Plan/root-cause: Claude Plan subagent (2026-08-05)
- Implementation: Claude general-purpose subagent ac2199ef00fc719aa, sonnet, worktree
  `task-150` (2026-08-05), commit `6bd81f13`
- Review round 1: Claude general-purpose subagent ab8a56c4e6accb230, sonnet (2026-08-05)
