# Review framework

## Budget (by-diff)

- Lines changed: 2335
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 489-001

**Date:** 2026-11-23
**Host task:** kanban/in-progress/489-001-generate-clone-and-from-i-details-copy-for-edit-contracts/
**Diff scope:** branch `task/489-001-generate-clone-and-from-i-details-copy-for-edit` vs `master`
(commits 72cf7b29e, 48e46bcb7, ba2d6cebc; 20 files, +2297 / −38)
**Plan / brief:** task.md `## Goal (implementer restatement)` and the original `## Description`;
parent 489 for the pattern.
**Effort:** 3 (by-diff budget); roster axis general only
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under `ganda task work` (headless; no vendor session id exposed)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
