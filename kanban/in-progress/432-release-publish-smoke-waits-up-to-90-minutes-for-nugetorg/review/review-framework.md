# Review framework

## Budget (by-diff)

- Lines changed: 181
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-29
**Host task:** kanban/in-progress/432-release-publish-smoke-waits-up-to-90-minutes-for-nugetorg/
**Diff scope:** branch `task/432-release-publish-smoke-waits-up-to-90-minutes-for-n` vs `master` merge base (product commit c3c952d29)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
