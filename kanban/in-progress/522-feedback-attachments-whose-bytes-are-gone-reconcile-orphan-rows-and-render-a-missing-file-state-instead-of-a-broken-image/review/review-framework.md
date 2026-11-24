# Review framework — task 522

**Date:** 2026-11-25
**Host task:** kanban/in-progress/522-feedback-attachments-whose-bytes-are-gone-reconcile-orphan-rows-and-render-a-missing-file-state-instead-of-a-broken-image/
**Diff scope:** branch `task/522-feedback-attachments-whose-bytes-are-gone` vs merge-base `40d680e8` (`master...HEAD`): 83 files, +1595/−734. Uncommitted kitchen files under `review/` are this round.
**Plan / brief:** Orphan feedback attachments (rows whose bytes were lost before task 352) report `IsFilePresent` from the byte store and render a missing-file state. In-memory feedback stores are removed. The feedback slice compiles out when the `postgres` template flag is off. Tests run on Postgres.
**Effort:** 3 (by-diff; single axis)
**Reviewer roster:** general
**Session IDs:** implementer claude `e556aad5-a349-4cf5-8ac5-947325901f05`; reviewer grok `01a20f5b-b48c-7613-8231-9dc945372089`

## Budget (by-diff)

- Lines changed: 2329
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
