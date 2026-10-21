# Review framework

## Budget (by-diff)

- Lines changed: 1399
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 393

**Date:** 2026-10-21
**Host task:** kanban/to-do/393-dev-deploy-migrate-builds-missing-migrations-bundle/
**Diff scope:** branch `task/393-dev-deploy-migrate-builds-missing-migrations-bundl` vs `master` (4 commits, fa58f65de..f981e7fc6); product paths `tools/dev-cli/`, `source/container-apps/aspire/projects/aspire-app-host/`, `tests/`, `skills/tw-deploy/`
**Plan / brief:** `## Goal (implementer restatement)` in task.md: one-command `dev deploy --target <t>` (publish → deploy → migrate → smoke → URL), missing bundle published by `dev deploy migrate`, ACA passkey ingress host, aca-neon region warning, skill/doc rewrite, dev-cli tests, live aca-neon proof
**Effort:** 3 (by-diff budget above); roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
