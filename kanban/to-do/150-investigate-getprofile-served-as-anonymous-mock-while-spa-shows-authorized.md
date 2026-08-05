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

## Session

- Created: Claude (2026-08-05)
