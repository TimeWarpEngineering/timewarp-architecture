# Review framework

## Budget (by-diff)

- Lines changed: 1042
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-26
**Host task:** kanban/in-progress/418-move-the-assistant-name-setting-from-the-authentication-admin-page-to-the-ai-page/
**Diff scope:** branch `task/418-move-the-assistant-name-setting-from-the-authentic` at `4339ef879`
vs merge-base `52fc38875` (master)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`
**Effort:** 3 (by-diff budget), roster general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by the host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
