# Review framework — task 245

**Date:** 2026-09-23
**Host task:** kanban/to-do/245-make-in-proc-test-ports-overridable-so-template-smoke-tier-3-does-not-collide-with-dev-test/
**Diff scope:** branch `task/245-make-in-proc-test-ports-overridable-so-template-sm` vs `origin/master` (commit 3999e734, 21 files)
**Plan / brief:** Replace the fixed in-proc test ports (web 7000 / web-http 7001 / api 7255 / yarp 8443) with a single resolved base (`InProcTestPorts`, `TIMEWARP_TEST_PORT_BASE`), derive every URL/port from it, have template-smoke tiers 2–3 run generated apps on a distinct base, and update AGENTS.md + the `tw-feature-placement` reference.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — Claude Fable 5.1, ganda task work headless (2026-09-23); gate re-verification delegated to a sonnet subagent in the same session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Gate claims in task.md Results are re-run here, not trusted (dev build; yarp suite under a non-default base, which tier 3 never exercises; 7255-listener override proof)
