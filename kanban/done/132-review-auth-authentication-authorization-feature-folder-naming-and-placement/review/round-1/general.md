# Round 1 — general
**Date:** 2026-09-04
**Scope reviewed:** branch task/132-review-auth-authentication-authorization-feature-f vs origin/feature/overnight (kanban inventory + naming disposition); verified against this worktree's source trees.

## Summary

Kanban-only naming disposition for the auth/authentication/authorization collision: inventory correctly records that `web/features/auth/` and `web/features/authentication/` are gone, `authorization/` is the 182 Features-substrate engine, `GetCurrentUser` is identity `[ClientOnlyContract]` (not who-am-I), and SPA still has `authentication/` + `account/` + `identity/` + `authorization/`. The six answers, glossary (bare Auth forbidden), reject/defer/do-now table, and child **132-001** (SPA fold into identity; keep `/authentication/{action}`, `/Login`, `/Logout`) resolve the remaining collision and are the right mechanical follow-on. One factual gap remains in the 118 host map: api already has a platform identity-host twin and does not yet reference the shared Features catalogs the disposition describes as shared today.

## Issues

### Issue 1 — Severity: bug
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/disposition.md:30-31,105; inventory.md:171-177
- Description: The 118 map overstates current sharing. Taxonomy calls the scheme-name catalog "web + api contracts" and describes api identity-host as "when it grows"; Q6 says `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` are "**shared contracts** — both families reference". In this tree, `source/container-apps/api/platform/identity-host/` already exists (`agent-token-defaults-server.cs`, `agent-token-authentication-handler-server.cs`, caller context, bearer stores module) with Design notes that `AgentTokenDefaults` **MUST stay byte-identical** to web's copy. Api does **not** reference those three Features substrate types (only a comment on the agent-bearer sample mentions `AuthenticationSchemeNames`; the sample authorizes with string/`AgentTokenDefaults` literals). Inventory §9 lists only `api/features/agent-bearer-sample/` and omits the live api identity-host cluster. That can mislead 118 into inventing another host/auth tree or assuming catalog wiring already exists.
- Suggestion: Correct taxonomy + Q6 to present tense: web owns `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` today; api already has `platform/identity-host/` (not `features/auth*`) with duplicated `AgentTokenDefaults` / handler parity; dual-host work should reuse the Features substrate catalogs rather than a third defaults copy or an `api/features/auth*` tree. Add `api/platform/identity-host/` to inventory §9 alongside the teaching sample.
- Status: open
