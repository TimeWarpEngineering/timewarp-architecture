# Review framework — task 506

**Date:** 2026-11-25
**Host task:** kanban/in-progress/506-analyzer-a-component-link-href-nav-link-anchor-fluent-anchor-is-a-state-navigate-action-or-an-opted-out-side-effect/
**Diff scope:** branch `task/506-analyzer-a-component-link-href-nav-link-anchor` vs `master` (merge-base `1c74108016d589cdd7e32d103b9ccfe966621414`, three commits, 56 files). Untracked kitchen files under `review/` are this round, not product.
**Plan / brief:** TWA0050 flags component markup links (`Href` / `href` / `To`, `NavLink`, `<a href>`) unless the link is an external scheme that opens a new tab or says `rel="external"`, or the component opts out with `[DirectComponentSideEffect("reason")]`. Web-spa hits become a State navigate action or an opted-out anchor. Roles list New Role is a catalogued `RoleState.OpenNewRole`. `skills/tw-blazor/SKILL.md` matches the analyzer.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** implementer claude `79a780c3-7854-40af-828e-d22e8d02fb0a`; reviewer grok `01a20e88-ab8c-7c82-9685-ab8a738853ae`

## Budget (by-diff)

- Lines changed: 1654
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
