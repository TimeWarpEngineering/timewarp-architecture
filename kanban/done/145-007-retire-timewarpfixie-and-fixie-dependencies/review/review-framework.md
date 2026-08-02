# Review framework — task 145-007

**Date:** 2026-08-02
**Host task:** kanban/in-progress/145-007-retire-timewarpfixie-and-fixie-dependencies/
**Diff scope:** commits since task start (`b3bfd42a`..`ad921367`) — Fixie retirement
**Plan / brief:** Migrate all remaining Fixie suites to Jaribu MTP; remove Fixie CPM + TimeWarpTestingConvention; MTP-only `dev test`; docs sweep; template-smoke green
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** current orchestrator session

## Ground rules

- Reviewers are read-only on product code; write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Zero issues is valid
- Re-verify falsifiable claims (no Fixie PackageReference, suites green, template-smoke)
