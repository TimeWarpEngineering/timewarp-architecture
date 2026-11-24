# Review framework — task 518

**Date:** 2026-11-25
**Host task:** kanban/in-progress/518-tw-web-api-contracts-skill-store-snapshot-generated-clone-and-from-working-copy/
**Diff scope:** branch `task/518-tw-web-api-contracts-skill-store-snapshot` vs `master` (merge-base `40d680e8`). Product diff: `skills/tw-web-api-contracts/SKILL.md`, `skills/tw-web-api-contracts/references/mutability.md`, `skills/tw-web-api-contracts/references/examples.md`, `skills/tw-blazor/SKILL.md`.
**Plan / brief:** task 518 description and requirements. Checked against `## Goal (implementer restatement)`.
**Effort:** round 1: 2 (general only). Round 2: 1 (general only); see Budget.
**Reviewer roster:** general
**Session IDs:** Round 1 reviewer Grok session `01a20ef9-9c19-71e3-894d-b01849941f8f` (2026-11-25). Implementer session `a5e5f07c-992b-42d1-9ffd-3f485ecc0563`. Round 2 reviewer Grok session `01a20f04-aa35-7e62-9457-ccf61ef3e915` (2026-11-25).

## Budget (by-diff)

Round 1:

- Lines changed: 422
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

Round 2 (fix delta):

- Lines changed: 177
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Restatement check

The restatement's "step 3 items 5 and 6" are the interface steps under `### 3. Bindable data` in `SKILL.md`. That matches the task's "interface steps 5 and 6". No misread recorded. Round 2 re-read the restatement against the task requirements. Still no misread.

## Round 2

**Date:** 2026-11-25
**Diff scope:** product delta since round 1 (`9bfe613cd`..`41fe221da`): `skills/tw-web-api-contracts/references/mutability.md` and `skills/tw-blazor/SKILL.md`. Re-checked M1–M3 against `AuthenticationPage.AcceptPolicy`, `RoleEditForm.OnParametersSet`, `RoleState`, and `documentation/developer/guides/edit-form-details-copy.md`.
**Effort:** 1 (general only)
**Session IDs:** Reviewer Grok session `01a20f04-aa35-7e62-9457-ccf61ef3e915` (2026-11-25).
