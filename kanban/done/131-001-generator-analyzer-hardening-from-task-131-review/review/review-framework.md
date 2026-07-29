# Review framework — task 131-001

**Date:** 2026-07-29
**Host task:** kanban/in-progress/131-001-generator-analyzer-hardening-from-task-131-review/
**Diff scope:** commit `bcce35a8` (feat analyzers harden) vs pre-task base; primary delta under `source/analyzers/`, AGENTS.md, ApiEndpointSourceGenerator.md, analyzer tests
**Plan / brief:** task.md Notes — F-003/004/005/008/014 implementation plan
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** tw-orchestrate-task 2026-07-29

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
