# Review framework

## Budget (by-diff)

- Lines changed: 357
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 269

**Date:** 2026-10-01
**Host task:** kanban/to-do/269-dev-db-nuke-removes-stopped-containers-that-hold-the-volume/
**Diff scope:** branch `task/269-dev-db-nuke-removes-stopped-containers-that-hold-t` vs `master`
(commit cfadcfa9): `tools/dev-cli/services/db-nuke.cs`, `tools/dev-cli/endpoints/db-nuke-command.cs`,
`tests/tools/dev-cli-tests/db-nuke-tests.cs`, `AGENTS.md`
**Plan / brief:** task.md Requirements 1–7. Before `docker volume rm`, `docker rm` the stopped containers that hold the volume. Refuse if any container is running. Include the containers in the dry listing.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (ganda task work, claude headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
