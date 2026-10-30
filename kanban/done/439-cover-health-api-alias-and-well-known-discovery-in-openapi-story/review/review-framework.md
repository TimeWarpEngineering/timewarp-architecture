# Review framework

## Budget (by-diff)

- Lines changed: 315
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 439

**Date:** 2026-10-29
**Host task:** kanban/in-progress/439-cover-health-api-alias-and-well-known-discovery-in-openapi-story/
**Diff scope:** commit 35c6b5749 (`feat: name health, the tip alias, and excluded discovery routes in OpenAPI`) on branch `task/439-cover-health-api-alias-and-well-known-discovery-in`. The other commits ahead of the stale local `master` are earlier merged PRs (#597–#599) and are out of scope.
**Plan / brief:** `task.md` `## Goal (implementer restatement)`: web-server OpenAPI includes `GET /api/health` and `GET|POST /api`, and lists the `/.well-known/*` discovery documents as excluded in `info.description` and `x-timewarp-excluded-routes`, cross-linked from `llms.txt` and the discovery skill.
**Effort:** 2 (budget by diff). Roster: general only.
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, ganda task work headless), 2026-10-29

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
