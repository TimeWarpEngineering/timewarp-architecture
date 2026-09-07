# Review framework — task 205-003

**Date:** 2026-09-07
**Host task:** kanban/in-progress/205-003-fix-profile-save-401-on-identity-session-put/
**Diff scope:** branch `task/205-003-fix-profile-save-401-on-identity-session-put` vs `origin/master` (product commit `48460f9a`; Results commit `2889015b`). Product files:

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` (new)
- `source/container-apps/web/projects/web-spa/services/browser-request-credentials-handler.cs` (new)
- `source/container-apps/web/projects/web-server/program.cs`
- `source/container-apps/web/projects/web-spa/program.cs`
- `source/container-apps/web/projects/web-spa/features/profiles/profile-state/profile-state.update-profile.cs`
- `source/foundation/foundation-contracts/services/http-api-service.cs`
- `source/container-apps/web/projects/web-server/hosted-identity-session-authentication-state-provider-server.cs` (Design only)
- `tests/container-apps/web/web-server-integration-tests/features/profile/update-profile-session-tests.cs` (new)
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs` (new)
- `tests/foundation/foundation-contracts-tests/http-api-service-tests.cs`

Kanban `task.md` Results are in scope only as the implementer brief, not product code.
**Plan / brief:** Profile Save PUT `api/Users/Current/Profile` returned 401 empty body (identity-session + mock-identity-session challenged) because InteractiveAuto/Server named HttpClient loopback and WASM fetch omitted the `.timewarp.identity.session` cookie. Fix: forward inbound Cookie + mock-principal header on server loopback; set WASM fetch credentials SameOrigin; map empty/non-JSON 401/403 to honest SharedProblemDetails; 401 Save toasts then `/Login?returnUrl=/Profile`. Do not turn `Authentication:UseMock` on. Anonymous PUT stays 401. Catalogs / identity kernel locked.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle session 01a079be-a8fe-7fb3-850d-72380e467e32 (2026-09-07)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
