# Review framework — task 513

**Date:** 2026-11-25
**Host task:** kanban/in-progress/513-brand-methodology-tw-brand-skill-brand-options-covers-every-brand-surface-one-appsettings-source-and-a-generated-app-shows-no-time-warp-brand/
**Diff scope:** Round 1 reviewed the branch vs merge-base `4b13bcf8969e063476e27d834bfc50f869d99545` (master): three commits, 98 files, about 3646 lines. Round 2 reviews the fix delta only, commit `27f8dc2fd`: `DesignSystemPage.razor`, `skills/tw-blazor-css-strategy/SKILL.md`, `brand-copy-tests.cs`, and the kitchen record of that fix.
**Plan / brief:** `task.md` requirements and `## Goal (implementer restatement)`. One server-owned `BrandOptions` section, role tokens, `tw-brand`, template-smoke rebrand check, evidence screenshots. Round 2 checks M1 and M2 against that fix.
**Effort:** Round 1 effort 3; round 2 effort 1. Roster is general only for both rounds.
**Reviewer roster:** general
**Session IDs:** Round 1 review oracle grok `01a20ec9-9a9a-7713-a38f-1427674d18bd` (2026-11-25). Round 2 review oracle grok `01a20ed8-08e5-7343-b16f-062062c524bd` (2026-11-25). Implementer session is on `task.md`.

## Budget (by-diff)

Round 2 (this review):

- Lines changed: 173
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

Round 1 (frozen under `review/round-1/`):

- Lines changed: 3646
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
