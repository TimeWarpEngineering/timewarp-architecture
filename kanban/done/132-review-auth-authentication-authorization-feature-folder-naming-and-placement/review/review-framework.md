# Review framework — task 132

**Date:** 2026-09-04
**Host task:** kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/
**Diff scope:** branch `task/132-review-auth-authentication-authorization-feature-f` vs `origin/feature/overnight` (commit `e3e74426` plus review-round fixes). Kanban-only: `task.md`, `inventory.md`, `disposition.md`. No product-code moves on this id. Child **132-001** is published on origin-home to-do (not in this worktree).

**Round 2 note:** re-verify M1 (118 map / api identity-host present tense) against the post-fix inventory + disposition. May scan the fix delta for new defects. Do not clobber `round-1/`.
**Plan / brief:** Naming + placement review of `web/features/{auth,authentication,authorization}` vs `identity/` and the 118 host split. Deliverables: complete inventory, six-question disposition with taxonomy + reject/defer/do-now, follow-on tasks only for accepted mechanical work.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle (2026-09-04)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Task-folder `disposition.md` is the **product** naming disposition (deliverable). Review outcome lives at `review/disposition.md`. Do not overwrite the product file.

## What to verify

Falsifiable claims in `inventory.md` / `disposition.md` against this worktree:

- `web/features/auth/` and `web/features/authentication/` absent; `authorization/` is the 182 engine; `RoleIds` under `admin/roles/` with `Features` substrate namespace.
- `GetCurrentUser` is `[ClientOnlyContract]` under `Features.Identity` with no handler; `GetCurrentSession` is the live who-am-I endpoint.
- SPA still has `authentication/`, `account/`, `identity/`, `authorization/`.
- Api family has no `auth*` / `identity` trees; `agent-bearer-sample` is teaching-only.
- Six brief questions answered; glossary forbids bare `Auth`; 118 map does not invent `api/features/auth*`.
- Child 132-001 exists on origin-home with parent 132 and depends-on 132; mechanical SPA fold only.
