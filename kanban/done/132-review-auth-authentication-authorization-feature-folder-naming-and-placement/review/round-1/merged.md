# Round 1 — merged findings
**Date:** 2026-09-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/disposition.md:30-31,105; inventory.md:171-177
- Description: The 118 map overstates current sharing. Taxonomy calls the scheme-name catalog "web + api contracts" and describes api identity-host as "when it grows"; Q6 says `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` are "**shared contracts** — both families reference". In this tree, `source/container-apps/api/platform/identity-host/` already exists (`agent-token-defaults-server.cs`, `agent-token-authentication-handler-server.cs`, caller context, bearer stores module) with Design notes that `AgentTokenDefaults` **MUST stay byte-identical** to web's copy. Api does **not** reference those three Features substrate types (only a comment on the agent-bearer sample mentions `AuthenticationSchemeNames`; the sample authorizes with string/`AgentTokenDefaults` literals). Inventory §9 lists only `api/features/agent-bearer-sample/` and omits the live api identity-host cluster. That can mislead 118 into inventing another host/auth tree or assuming catalog wiring already exists.
- Suggestion: Correct taxonomy + Q6 to present tense: web owns `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` today; api already has `platform/identity-host/` (not `features/auth*`) with duplicated `AgentTokenDefaults` / handler parity; dual-host work should reuse the Features substrate catalogs rather than a third defaults copy or an `api/features/auth*` tree. Add `api/platform/identity-host/` to inventory §9 alongside the teaching sample.
- Source: general
- Disposition notes: Fixed on this id (inventory §9 + taxonomy + Q6 + reject/defer). Api `platform/identity-host/` documented as already live; catalogs described as web-owned today; dual-host = reuse, not a third `AgentTokenDefaults` or `api/features/auth*`.

## Duplicates / conflicts

- None (single reviewer).
