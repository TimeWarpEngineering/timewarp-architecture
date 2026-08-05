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

- [ ] Reproduce: `dev run`, sign in (or observe default state), open Profile page; capture the
      GetProfile request (cookie/Authorization header presence) and the server-side log/claims
- [ ] Determine what `AuthenticationStateProvider` implementation is active in the SPA for
      this run mode and why it reports authorized
- [ ] Determine what populates `ICurrentUserService.UserId` server-side and why it was null
      for this request
- [ ] Assess `55ee9384` (UseMock no longer forced in Dev) for its role; document intended Dev
      auth posture
- [ ] Implement the fix on the correct side (client auth state vs server principal flow)
- [ ] Add regression test for the authorized-SPA-gets-mock-profile failure mode
- [ ] `dev build` 0/0; affected test suites green
- [ ] Results with How to validate

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

## Session

- Created: Claude (2026-08-05)
