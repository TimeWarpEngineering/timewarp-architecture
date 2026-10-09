# Round 1 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:40
- Description: The lean core dropped the task 124 rule that a version bump also bumps the platform CPM pins in the same commit.
- Suggestion: Add a clause to the PR-gates row.
- Source: general
- Disposition notes: The `Before a PR` row now says "bump `<Version>` and the platform package pins in the same commit". Size numbers in task.md and audit.md updated to 3,566 / 54 / 481.

### M2 — Severity: nit — Status: wontfix
- File: skills/tw-feature-placement/references/co-located-jaribu-runfiles.md:73
- Description: The Host lanes table does not repeat `TIMEWARP_TEST_PORT_BASE` or the default ports.
- Suggestion: Mention the override in the in-proc row.
- Source: general
- Disposition notes: Already discoverable. The same reference names `TIMEWARP_TEST_PORT_BASE` at lines 11–12 and 23, and the table points at `InProcTestPorts`, which owns the defaults. Repeating them would add a second copy that can drift. Decided by: review oracle.

### M3 — Severity: nit — Status: fixed
- File: kanban/to-do/294-audit-and-trim-agentsmd-to-a-lean-core/task.md:61
- Description: The word "kitchen" is unclear in Results.
- Suggestion: Reword it.
- Source: general
- Disposition notes: Changed to "in this task folder" and "the host's task-folder path".

### M4 — Severity: nit — Status: fixed
- File: skills/tw-feature-placement/SKILL.md:257
- Description: A reflowed paragraph left a short orphan line.
- Suggestion: Rewrap it.
- Source: general
- Disposition notes: Rewrapped.

## Duplicates / conflicts

- None (single reviewer).
