# Review framework — task 511

**Date:** 2026-11-24
**Host task:** kanban/in-progress/511-feedback-attachments-replace-the-bare-input-file-with-the-fluent-ui-file-control-and-space-the-attachment-actions-per-the-design-system/
**Diff scope:** branch `task/511-feedback-attachments-replace-the-bare-input-file` vs `master` (merge-base `3ed7d19f51f5c72fdec8150505e50a29f5dc9ff4`). Product and test changes are in `8707e45a4`. Kitchen review files are untracked.
**Plan / brief:** Replace the Feedback page's bare `InputFile` with a hidden `FluentInputFile` opened from an outline "Choose files" button, and space the attachment actions and chips with design-system tokens. Six light/dark evidence shots.
**Effort:** 2 (general only; no specialist axis)
**Reviewer roster:** general
**Session IDs:** implementer claude `1cd22e93-88ae-4755-880e-0060d4a7e266`; review grok `01a20c5f-5555-72e0-95d3-4e4080e9423c`

## Budget (by-diff)

- Lines changed: 458
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
