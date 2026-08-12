# Claude pickup — task 183

**From:** Claude (implementer + reviewer)
**Date:** 2026-08-12
**Worktree:** `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/dev` (branch `dev`)

## Accept

Accepting the ask as scoped in `request-for-assistance-claude.md`, with one scope correction:

- **Ask #2 (commit) is already done.** The working tree is clean and the fix + kitchen landed as:
  - `e55bcabf` — fix(web): stop web-server stack overflow on authorized page prerender
  - `33e616f3` — fix(web): stop web-server prerender stack overflow; kitchen for 183
  - `e84feb39` — chore(kanban): finish 183 folderize by removing flat task file

  So the "commit hung on hooks" state resolved itself before this pickup; no re-commit needed.

## Plan

1. Review the landed diff (hosted auth-state provider, program.cs re-registration, unsealed SPA provider, RedirectToLogin SSR guard) for correctness / better patterns.
2. Restart Aspire cleanly; verify anonymous and (as far as automatable) authenticated `/Admin/Roles` no longer kills web-server.
3. Update task checklist, write `## Results` + `### How to validate`, move 183 to done.

Findings will be appended as `notes/claude-review.md`; verification outcome in task `## Results`.

## Outcome (same day)

- **Review:** Accept — `notes/claude-review.md` (one non-blocking InteractiveAuto circuit observation).
- **Scope addition:** live verification surfaced **root cause 4** — authenticated SSR 500'd
  deterministically under postgres because concurrent policy evaluations (AuthorizeRouteView +
  nav AuthorizeViews) raced the scoped `PostgresDbContext` via
  `PermissionRequirementHandler → IPermissionEvaluator → EF stores`. Fixed by single-flighting
  the per-(principal, scheme) expansion inside the scoped `PermissionEvaluator`.
  In-memory stores were immune, which is why the in-proc suite alone couldn't have caught it.
- **Verification:** live admin `/Admin/Roles` + `/Settings` 200 (were 500), anonymous 302 to
  Login, web-server Running/Healthy throughout; in-proc regression tests added
  (ProtectedPageDeepLink_ Ok_Page_*, evaluator ConcurrentChecks single-flight); gates green
  (dev build 0/0, integration suite 125/126 w/ 1 intentional skip, web aggregator 86/86).
- Full detail: task `## Results`. Commit SHA recorded there and in git history on `dev`.
