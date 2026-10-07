# Review framework

## Budget (by-diff)

- Lines changed: 735
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 284

**Date:** 2026-10-07
**Host task:** kanban/to-do/284-dev-db-nuke-delegates-to-aspire-stop-volumes-remove-reconstructed-aspire-volume-names-and-docker-sweep/
**Diff scope:** branch `task/284-…` vs `master` (commit 8a2d79366; 7 files, +150/−585)
**Plan / brief:** task.md Requirements — nuke = CLI ≥13.6 preflight + `aspire stop --force --volumes` + manual adopted-volume hint; delete hash/sweep/container-cleanup code and tests
**Effort:** 2 (by-diff budget) — roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (ganda task work, Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
