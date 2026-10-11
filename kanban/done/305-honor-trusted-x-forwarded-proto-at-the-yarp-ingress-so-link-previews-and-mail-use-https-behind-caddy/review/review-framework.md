# Review framework

## Budget (by-diff)

- Lines changed: 772
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 305

**Date:** 2026-10-11
**Host task:** kanban/to-do/305-honor-trusted-x-forwarded-proto-at-the-yarp-ingress-so-link-previews-and-mail-use-https-behind-caddy/
**Diff scope:** branch `task/305-honor-trusted-x-forwarded-proto-at-the-yarp-ingres` vs `master` (head `bc16af0f3`)
**Plan / brief:** task.md Requirements 1–6. Trusted-proxy forwarded headers on the YARP ingress; run mode hosts the yarp project.
**Effort:** 2 (by-diff budget). Roster axes: general.
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** ganda task work review oracle (Claude Code, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
