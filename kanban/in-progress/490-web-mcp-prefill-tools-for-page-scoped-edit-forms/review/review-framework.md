# Review framework

## Budget (by-diff)

- Lines changed: 712
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope (task 490)

**Date:** 2026-11-24
**Host task:** kanban/in-progress/490-web-mcp-prefill-tools-for-page-scoped-edit-forms/
**Diff scope:** branch `task/490-web-mcp-prefill-tools-for-page-scoped-edit-forms` (HEAD 5cb923a25) vs merge-base 039361895 on master
**Plan / brief:** task.md `## Description` and `## Goal (implementer restatement)`: `Prefill.<Action>` tools for `[EditFormSubmit]` page-scoped submit actions, a stored suggestion applied by the page, a dirty-form confirm, and an Agent badge. Profile is the only consumer.
**Effort:** 3 (roster axis: general)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Code, claude-opus-5-5), headless `ganda task work` review node

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
