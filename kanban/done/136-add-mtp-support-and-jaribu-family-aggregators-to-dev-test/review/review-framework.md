# Review framework — task 136

**Date:** 2026-07-31
**Host task:** kanban/in-progress/136-add-mtp-support-and-jaribu-family-aggregators-to-dev-test/
**Diff scope:** commit `52dda114` (feat: MTP-aware dev test and Jaribu family aggregators) vs parent; plan at `plan.md`
**Plan / brief:** MTP detect + bare cwd `dotnet test`; web/api JARIBU_MULTI aggregators; TestingPlatform pin; template-smoke tier 3; docs
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestration 136; implementer 019fb590-3c2e-7f51-938b-d15612d3be63

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
