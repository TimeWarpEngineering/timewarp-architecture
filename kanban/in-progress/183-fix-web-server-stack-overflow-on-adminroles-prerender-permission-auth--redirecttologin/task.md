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

DB seeds and role assignments were fine (not the bug).

## Fix (landed in worktree; commit + live verify pending)

- `HostedIdentitySessionAuthenticationStateProvider` (web-server): prefer `HttpContext.User` when authenticated.
- Unseal SPA `IdentitySessionAuthenticationStateProvider` for inheritance.
- `RedirectToLogin`: navigate only when interactive; static link fallback.

## Checklist

- [x] Diagnose via Aspire MCP / `aspire logs web-server`
- [x] Prefer HttpContext.User on hosted prerender
- [x] SSR-safe RedirectToLogin
- [x] `dotnet build web-server` 0/0
- [x] Folderize task; write Claude assistance request in `notes/`
- [ ] Commit (kanban + code)
- [ ] Restart Aspire and confirm web-server stays Running after authenticated `/Admin/Roles`
- [ ] Results + How to validate → done

## Notes

- Full handoff for Claude: `notes/request-for-assistance-claude.md`
- Append progress / pickup replies under `notes/` (do not replace the assistance brief).

## Session

- Grok: diagnosis + fix implementation (2026-08-12); AppHost restart timed out after stop; paused for Claude assist via kitchen.
