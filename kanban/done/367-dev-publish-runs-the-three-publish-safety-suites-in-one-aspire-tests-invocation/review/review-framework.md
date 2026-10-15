# Review framework

## Budget (by-diff)

- Lines changed: 453
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 367

**Date:** 2026-10-15
**Host task:** kanban/to-do/367-dev-publish-runs-the-three-publish-safety-suites-in-one-aspire-tests-invocation/
**Diff scope:** branch task/367 vs fa42c1a40 (commit ffbd636fe): tools/dev-cli publish commands + services, workflow-command, dev-cli-tests, workflow.yml comment, tw-deploy skill
**Plan / brief:** `dev publish all [--no-build]` — three `aspire publish` runs then one aspire-tests run with all TIMEWARP_*_OUTPUT set; `dev workflow` uses it
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** not reported by harness

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
