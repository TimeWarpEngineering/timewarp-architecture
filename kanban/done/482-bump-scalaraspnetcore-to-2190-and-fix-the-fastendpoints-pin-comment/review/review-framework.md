# Review framework

## Budget (by-diff)

- Lines changed: 120
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 482

**Date:** 2026-11-05
**Host task:** kanban/in-progress/482-bump-scalaraspnetcore-to-2190-and-fix-the-fastendpoints-pin-comment/
**Diff scope:** branch `task/482-bump-scalaraspnetcore-to-2190-and-fix-the-fastendp` vs `master`
(product change: `Directory.Packages.props` only; the rest is kanban moves)
**Plan / brief:** Scalar.AspNetCore 2.18.7 -> 2.19.0; correct the comment above the FastEndpoints
pins so it names FastEndpoints.OpenApi as the package depending on Microsoft.AspNetCore.OpenApi.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work headless

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
