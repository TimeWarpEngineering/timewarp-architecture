# Review framework

## Budget (by-diff)

- Lines changed: 676
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

- **Date:** 2026-10-08
- **Host task:** kanban/to-do/286-dev-deploy-deploy-config-and-validation-checks-instead-of-aspire-parameter-prompts/
- **Diff scope:** branch `task/286-dev-deploy-deploy-config-and-validation-checks-ins` vs `master` (implement commit 93c6a653b; fix commits after it)
- **Plan / brief:** task.md Requirements — per-target deploy config forwarded as `--Parameters:*`, kubernetes preflight checks, TTY passthrough, tests, tw-deploy skill
- **Reviewer roster:** general (subagent, Claude Opus 5.5)
- **Session IDs:** review oracle under `ganda task work` (headless Claude); reviewer subagents aa1b8e5e244d9df8d (round 1), ace0b5c0c3151748d (round 2); fix implementer a9716e483b7c14928

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
