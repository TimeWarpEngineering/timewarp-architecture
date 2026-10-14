# Review framework

## Budget (by-diff)

- Lines changed: 1056
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 352

**Date:** 2026-10-14
**Host task:** kanban/to-do/352-feedback-attachment-bytes-are-durable-via-postgres-when-no-blob-storage-is-configured/
**Diff scope:** branch task/352-… vs master (commit 965cf28b6)
**Plan / brief:** Postgres bytea attachment byte store; mode selection Azure → Postgres → in-memory; orphan 404; tests; docs.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
