# Review framework

## Budget (by-diff)

- Lines changed: 669
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-23
**Host task:** kanban/in-progress/489-002-convert-profile-edit-form-to-whole-object-state-and-generated-copy/
**Diff scope:** commit ce52debcd (`feat: keep the profile form on a copy of the fetched response`) on
branch `task/489-002-convert-profile-edit-form-to-whole-object-state`. The merge of
`feature/edit-forms-whole-object` (489-001, PR #657) is the base and was not reviewed here.
**Plan / brief:** task.md `## Description` and `## Goal (implementer restatement)`.
**Effort:** 2 (by-diff budget), roster: general only.
**Reviewer roster:** general
**Session IDs:** review oracle Claude Opus 5.5 (claude-opus-5-5), 2026-11-23.

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
