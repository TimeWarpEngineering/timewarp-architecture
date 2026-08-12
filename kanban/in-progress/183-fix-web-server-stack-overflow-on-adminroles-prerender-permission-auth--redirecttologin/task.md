# fix: web-server stack overflow on /Admin/Roles prerender (permission auth + RedirectToLogin)

## Description

`web-server` crashed with **exit 134 (stack overflow)** when a signed-in admin hit `/Admin/Roles` via Aspire. AppHost stayed up; MCP showed web-server `Finished` / exit 134.

**Kitchen:** folder task — multi-agent notes under `notes/`.  
**Assistance request (Grok → Claude):** [`notes/request-for-assistance-claude.md`](notes/request-for-assistance-claude.md)

## Root cause

1. **Prerender auth state was always anonymous** for cookie sessions.  
   `IdentitySessionAuthenticationStateProvider` HTTP-calls `GET api/identity/session` via named `HttpClient`. Loopback does **not** forward the browser `.timewarp.identity.session` cookie, so session looked unauthenticated during SSR even when `HttpContext.User` was a valid admin.

2. Hosted DI keeps **PermissionRequirement** policies (not SPA claim policies).  
   `AuthorizeRouteView` failed `PermissionRequirement` for every `[Authorize]` page during prerender.

3. **`RedirectToLogin.NavigateTo` during static SSR** of the interactive root nested render modes and **stack-overflowed** the process (~100k frames).

4. **(Found during live verification of 1–3)** With the crash gone, authenticated SSR got far
   enough to expose a second defect: Blazor evaluates several policies **concurrently** in one
   request scope (`AuthorizeRouteView` + nav `AuthorizeView`s), each going
   `PermissionRequirementHandler → IPermissionEvaluator → EF stores` on the **same scoped
   `PostgresDbContext`** → EF "A second operation was started on this context" → every
   authenticated page **500'd deterministically** under postgres (in-memory stores were immune,
   which is why in-proc tests passed). Verified live: admin `/Admin/Roles` and Member `/Settings`
   both 500 pre-fix, 200 post-fix.

DB seeds and role assignments were fine (not the bug).

## Fix

- `HostedIdentitySessionAuthenticationStateProvider` (web-server): prefer `HttpContext.User` when authenticated.
- Unseal SPA `IdentitySessionAuthenticationStateProvider` for inheritance.
- `RedirectToLogin`: navigate only when interactive; static link fallback.
- `PermissionEvaluator`: single-flight the per-(principal, scheme) expansion within the scope —
  concurrent policy checks share ONE sequential DB chain (fixes root cause 4; also removes N
  duplicate role/permission queries per render). Scoped-lifetime semantics unchanged
  ("rebundle takes effect next request").

## Checklist

- [x] Diagnose via Aspire MCP / `aspire logs web-server`
- [x] Prefer HttpContext.User on hosted prerender
- [x] SSR-safe RedirectToLogin
- [x] `dotnet build web-server` 0/0
- [x] Folderize task; write Claude assistance request in `notes/`
- [x] Commit (kanban + code) — landed as e55bcabf / 33e616f3 / e84feb39 (hook hang resolved itself)
- [x] Claude review of the fix → `notes/claude-review.md` (verdict: Accept)
- [x] Regression tests: authenticated HTML SSR of `/Settings` (Member) and `/Admin/Roles`
      (Administrator) return 200 in-proc — pre-183 this path stack-overflowed the process
      (was the documented residual in protected-page-deep-link-tests.cs)
- [x] Fix DbContext-concurrency 500 on authenticated SSR (root cause 4) + single-flight
      regression test in permission-evaluator-tests.cs
- [x] Suites green: web-server-integration-tests 125/126 (1 intentional skip),
      web-jaribu-tests aggregator 86/86, permission-evaluator runfile 12/12,
      permission-claim-policies runfile 6/6; `dev build` 0/0
- [x] Restart Aspire and confirm web-server stays Running after authenticated `/Admin/Roles`
- [x] Results + How to validate → done

## Results

The crash and the follow-on 500 are both fixed and verified live against the running Aspire
topology (postgres lane) and in-proc:

- **Live (Aspire `dev run`, web-server https://localhost:63611, postgres store):**
  - Anonymous `/Admin/Roles` (HTML) → `302 /Login?returnUrl=%2FAdmin%2FRoles`; server stays Running.
  - Passkey-ceremony session cookie minted against the live server (software authenticator,
    throwaway principal); principal elevated to Administrator via direct DB insert.
  - Administrator `/Admin/Roles` → **200 × 3**, body contains `data-qa="NewRole"`, zero
    occurrences of the RedirectToLogin fallback. Administrator `/Settings` → **200 × 3**.
  - web-server resource **Running / Healthy** after all hits; no `Stack overflow`, no exit 134.
  - Before the PermissionEvaluator fix the same requests returned **500 deterministically**
    (EF DbContext concurrency — see root cause 4), proving both halves were needed.
  - Throwaway smoke principals deleted from the dev DB afterwards (principals count back to 1).
- **In-proc regression tripwires** (would have killed the test process pre-183):
  `ProtectedPageDeepLink_` `Ok_Page_Given_Passkey_Member_Settings_Html` and
  `Ok_Page_Given_Passkey_Administrator_Admin_Roles_Html` — 200 + real page body asserted.
- **Evaluator single-flight proof:** `ConcurrentChecks_Should_SingleFlightStoreExpansion`
  (4 concurrent checks → exactly 1 store expansion, max concurrency 1).
- **Gates:** `dev build` 0/0; web-server-integration-tests 125 pass / 1 intentional skip;
  web family JARIBU_MULTI aggregator 86/86.
- Review verdict + a non-blocking InteractiveAuto server-circuit observation:
  `notes/claude-review.md`.

### How to validate

```bash
dev build                                            # 0/0
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release
#   → ProtectedPageDeepLink_ Ok_Page_* are the 183 tripwires (pre-fix: process death)
dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs
#   → ConcurrentChecks_Should_SingleFlightStoreExpansion (pre-fix: >1 concurrent store query)

# Live (needs running Aspire: dev run)
# 1. Anonymous:  GET /Admin/Roles with Accept: text/html → 302 /Login?returnUrl=…
# 2. Signed-in admin (browser passkey session): open /Admin/Roles → Roles page renders (200)
# 3. aspire describe web-server → Running/Healthy; aspire logs web-server → no "Stack overflow"
```

## Notes

- Full handoff for Claude: `notes/request-for-assistance-claude.md`
- Claude review of Grok's fix: `notes/claude-review.md` (Accept; InteractiveAuto circuit
  observation is non-blocking, revisit only if signed-in users ever bounce to Login during the
  server-interactive window — durable fix would be auth-state serialization)
- Append progress / pickup replies under `notes/` (do not replace the assistance brief).

## Session

- Grok: diagnosis + fix implementation (2026-08-12); AppHost restart timed out after stop; paused for Claude assist via kitchen.
- Claude (2026-08-12): picked up per brief — confirmed Grok's commits landed (e55bcabf/33e616f3/e84feb39
  were already on dev); reviewed fix (Accept, `notes/claude-review.md`); added in-proc authenticated-SSR
  regression tests (resolving task 154's documented residual); live-verified via Aspire and found root
  cause 4 (scoped DbContext concurrency 500 under postgres); fixed via PermissionEvaluator single-flight
  + regression test; re-verified live (200s, Running/Healthy); cleaned up throwaway smoke principals.
