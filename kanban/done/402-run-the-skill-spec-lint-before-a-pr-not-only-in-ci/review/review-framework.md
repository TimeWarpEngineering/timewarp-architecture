# Review framework

## Budget (by-diff)

- Lines changed: 750
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/402-run-the-skill-spec-lint-before-a-pr-not-only-in-ci/
**Diff scope:** commit e653349df vs d5c6f55c8 (task 402 implementation). Local `master` (e5e8245bb) is stale and predates the task 403 merge, so `master...HEAD` also shows 403's files. Those are out of scope.
**Plan / brief:** `dev lint-skills` runs `vally lint skills` plus `vally lint --eval-spec` for each skill eval spec, at the pin in `tools/dev-cli/vally-cli.version`. CI installs that pin and calls the verb. AGENTS.md "Before a PR" row names the verb.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work), session id not exposed to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
