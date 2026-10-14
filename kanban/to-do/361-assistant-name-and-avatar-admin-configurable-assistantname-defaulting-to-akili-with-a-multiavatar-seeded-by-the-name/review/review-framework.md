# Review framework

## Budget (by-diff)

- Lines changed: 1776
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 361

**Date:** 2026-10-14
**Host task:** kanban/to-do/361-assistant-name-and-avatar-admin-configurable-assistantname-defaulting-to-akili-with-a-multiavatar-seeded-by-the-name/
**Diff scope:** commit 639643d5f (fe4dec7e0..HEAD on task/361-…); 301 commits below it are already reviewed under task 301
**Plan / brief:** task.md Requirements + Amendment (builds on 301); Results describe what shipped
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5); reviewer sub-agent (claude)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
