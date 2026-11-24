# Review framework — task 508

**Date:** 2026-11-24
**Host task:** kanban/in-progress/508-prefill-every-edit-form-wire-role-ai-authentication-and-role-create-into-the-490-prefill-tools-and-collapse-the-per-form-boilerplate/
**Diff scope:** branch `task/508-prefill-every-edit-form-wire-role-ai` vs merge-base with `master` (`32ccc551`). Three commits. Product diff is 32 files (form prefill, the five edit forms, Profile migration, convention and page tests, `tw-blazor`). Kanban `task.md` is on the branch. `review/` is untracked kitchen.
**Plan / brief:** Wire Role update, Role create, AI (two tools), and Authentication into the task 490 prefill tools. One suggestion store and one shared page component. Derive the opt-in from a page-scoped submit whose Command implements one `I*Details` interface, and delete `[EditFormSubmit]`. Passkeys rename is out when it is not that shape. A convention test fails an unwired derived submit.
**Effort:** 3 (by-diff budget; roster axis is general only)
**Reviewer roster:** general
**Session IDs:** implementer Claude `5f10deb9-8939-414b-8d64-765f03250951`; reviewer Grok `01a20e1d-6ad8-7190-a625-7d15a7f2d866`

## Budget (by-diff)

- Lines changed: 2295
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
