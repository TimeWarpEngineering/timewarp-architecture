# Review framework

## Budget (by-diff)

- Lines changed: 393
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 339

**Date:** 2026-10-13
**Host task:** kanban/to-do/339-dev-deploy-migrate-reports-which-migrations-were-applied-instead-of-the-raw-psql-transcript/
**Diff scope:** branch task/339-… vs master (implement commit 53aad60cb): tools/dev-cli deploy-operate + deploy-migrate-command, dev-cli-tests, skills/tw-deploy
**Plan / brief:** task.md Requirements: report applied migrations from __EFMigrationsHistory before/after, quiet psql, --verbose, ACA summary line
**Effort:** 2 (budget by-diff), roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
