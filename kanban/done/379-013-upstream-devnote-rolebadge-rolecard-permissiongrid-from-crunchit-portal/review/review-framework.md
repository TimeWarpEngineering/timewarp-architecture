# Review framework

## Budget (by-diff)

- Lines changed: 1302
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 379-013

**Date:** 2026-10-24
**Host task:** kanban/in-progress/379-013-upstream-devnote-rolebadge-rolecard-permissiongrid-from-crunchit-portal/
**Diff scope:** branch `task/379-013-upstream-devnote-rolebadge-rolecard-permissiongrid` vs `master` (product commit `bb35ac1f9`; 20 files, +1030 / −22)
**Plan / brief:** task.md Checklist and `## Goal (implementer restatement)` — DevNote, RoleBadge, RoleCard, PermissionGrid (+ PermissionGroup / PermissionItem), StatusBadge attribute passthrough, `--twe-on-positive` token, style-guide cards, HtmlRenderer tests
**Effort:** 3 (roster axes: general)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, model claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Scope — round 3

**Date:** 2026-10-24
**Diff scope:** merge `d94228528` (`feature/379-portal-rest` into the task branch) and `origin/feature/379-portal-rest..HEAD`, which is the delta for PR #568 (18 product files, +952 / −8). The by-diff budget counts `master...HEAD` (1302 lines), and that range includes the 379-012 commits.
**Effort:** 3 (roster axes: general)
**Reviewer roster:** general
**Session IDs:** review oracle round 3 (claude, model claude-opus-5-5), session not reported
