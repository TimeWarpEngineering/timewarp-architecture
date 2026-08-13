# Request for assistance — Claude

**From:** Grok (orchestrator / implementer on this worktree)  
**To:** Claude  
**Task:** **183** (folder kitchen — append replies under `notes/`)  
**Worktree:** `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/dev`  
**Branch:** `dev`  
**Date:** 2026-08-12  

Please pick this up as implementer (or dual implementer + advisor). Use this folder as the kitchen: append notes here, do not invent parallel chat-only history.

---

## Ask

1. **Review** the uncommitted fix for correctness / better patterns (Blazor Web App prerender + cookie auth).
2. **Commit** task 183 + code (Grok’s commit hung on pre-commit/hooks in-session).
3. **Restart** Aspire cleanly and **verify** signed-in `/Admin/Roles` no longer kills `web-server`.
4. Mark **183 done** only with `## Results` + `### How to validate` (see `tw-agent-collaboration`).

Optional stretch (only if review says the hosted provider is incomplete):

- Cookie-forwarding handler for session loopback during prerender, *or*
- Enrich prerender principal with `PermissionIds.ClaimType` claims for claim-policy parity (hosted still uses `PermissionRequirement` today).

---

## What failed (Aspire MCP)

**Not** the Aspire MCP server itself. **`web-server` crashed** with exit **134** (stack overflow).

| Resource | State when failing |
|----------|--------------------|
| AppHost / dashboard | Running |
| api-server, grpc-server, postgres, ingress | Running / Healthy |
| **web-server** | **Finished**, exit **134** |
| web-migrations | Finished OK (exit 0) |

MCP notes:

- `aspire__list_resources` initially said “No AppHost running” until `aspire__select_apphost` with:
  `source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj`
- After select, list_resources / console logs worked.
- CLI: `aspire describe --include-hidden`, `aspire logs web-server`.

---

## Symptom (logs)

Signed-in admin hit **`/Admin/Roles`** (ingress host `arch.timewarp.work` or local https):

```text
Authorization failed. These requirements were not met:
TimeWarp.Architecture.Features.PermissionRequirement
Stack overflow.
   at … DataProtection … Protect …
   at … ServerComponentSerializer.SerializeInvocation …
   at … SSRRenderModeBoundary.ToMarker …
   at … StaticHtmlRenderer.WriteComponentHtml / RenderChildComponent … (repeats ~100k lines)
```

Process exits → Aspire shows web-server **Finished**.

**Not a crash path:** anonymous `/Admin/Roles` → cookie challenge **302** to Login (no stack overflow). Crash needs **cookie present** + Blazor prerender **NotAuthorized** path.

---

## DB check (red herring)

Seeds and membership were **correct** when crash reproduced:

- `identity.role_permissions` has Administrator grants including `admin.roles.read`
- Principal had Administrator + Member in `identity.principal_roles`

So this is **not** “empty role_permissions after wipe.”

---

## Root cause (cascade)

### A — Prerender auth state always anonymous for cookie sessions

Hosted DI composes SPA `IdentitySessionAuthenticationStateProvider` for `CascadingAuthenticationState` / `AuthorizeRouteView`.

That provider calls **`GET api/identity/session`** via named `HttpClient` (`IWebServerApiService`). The loopback request **does not forward** the browser cookie `.timewarp.identity.session`.

During SSR:

- `HttpContext.User` = valid cookie principal (`identity-session` + `timewarp:principal_id` + role claims from `PrincipalRoleClaimsTransformation`)
- SPA `AuthenticationState` = **anonymous** (session API returns unauthenticated)

Hosted policies stay **`PermissionRequirement`** (SPA skips claim policies when server already registered `AdminAccess` — see SPA `policy-registration.cs`). So prerender `[Authorize(Policy = admin.roles.read)]` fails even for a real admin.

### B — RedirectToLogin kills the process

`Routes.razor` `NotAuthorized` → if not authenticated → `<RedirectToLogin/>` → **`NavigationManager.NavigateTo` in `OnInitialized` during static SSR** of the interactive root (`Routes @rendermode=InteractiveAuto`).

That nested interactive SSR boundaries → **stack overflow** → exit 134.

---

## Fix already in the worktree (uncommitted)

| File | Change |
|------|--------|
| `source/container-apps/web/projects/web-server/hosted-identity-session-authentication-state-provider-server.cs` | **New.** Prefer `HttpContext.User` when authenticated; else base session HTTP. |
| `source/container-apps/web/projects/web-server/program.cs` | After `Web.Spa.Program.ConfigureServices`, re-register `Hosted…` only when SPA registered `IdentitySessionAuthenticationStateProvider` (not mock/Entra). |
| `source/container-apps/web/projects/web-spa/services/identity-session-authentication-state-provider.cs` | Unsealed so hosted type can inherit; Design region documents task 183. |
| `source/container-apps/web/projects/web-spa/services/identity-session-authentication-registration.cs` | Design note only. |
| `source/container-apps/web/projects/web-spa/pages/RedirectToLogin.razor` | Navigate only when `RendererInfo.IsInteractive` in `OnAfterRender` + `forceLoad: true`; static `<a>` fallback. |

**Build:** `dotnet build source/container-apps/web/projects/web-server/web-server.csproj -c Debug --no-restore` → **0/0**.

**Not verified live:** after `aspire stop`, `aspire start` timed out repeatedly on “Building AppHost…” (120–600s) with no build output — MSBuild / NuGet contention. Kill stuck MSBuild nodes if needed, then start.

---

## Suggested validation (for Results)

```bash
# Clean start
aspire stop   # if needed
# if hung builds: pgrep -af MSBuild | … careful kill
aspire start  # or ASPIRE_CLI_START_TIMEOUT=600 / dev run

# Smoke — anonymous should not crash web-server
# GET /Admin/Roles without cookie → 302 Login (or Sign in link), web-server still Running

# Smoke — signed-in admin (browser with passkey session)
# open /Admin/Roles → page or Forbidden, NOT process death
aspire describe web-server   # expect Running / Healthy
aspire logs web-server | rg -i 'Stack overflow|exit'   # expect none new
```

Expect: no `Stack overflow`; no exit 134; Admin Roles works for first-admin principal.

---

## Related context

- Parent epic **182** permission-centric authz (PermissionIds, evaluator, SPA claim policies guarded against overwriting server policies).
- Cookie name / claim: `IdentitySessionDefaults` (`CookieName`, `PrincipalIdClaimType = timewarp:principal_id`, scheme `identity-session`).
- Page: `[Page("/Admin/Roles", Policy = PermissionIds.AdminRolesRead)]` + `[Authorize(Policy = …)]`.

---

## Reply here

Claude: please append a short note under `notes/` (e.g. `notes/claude-pickup.md` or continue this file) when you start, with:

- Accept / scope change
- Session id if available
- Commit SHA when landed
- Verification outcome
