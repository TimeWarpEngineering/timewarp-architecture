# Review framework — task 254

**Date:** 2026-09-29
**Host task:** kanban/to-do/254-seed-site-settings-on-first-read-not-only-at-web-server-boot/
**Diff scope:** branch `task/254-seed-site-settings-on-first-read-not-only-at-web-s` vs `master` (commits 0d141066, e61783ce)
**Plan / brief:** task.md Requirements — seed-on-read decorator over `ISiteSettingsStore`, isDevelopment propagation, concurrency, 42P01 handling, 503 "until seeded" removed, tests.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (headless Claude)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
