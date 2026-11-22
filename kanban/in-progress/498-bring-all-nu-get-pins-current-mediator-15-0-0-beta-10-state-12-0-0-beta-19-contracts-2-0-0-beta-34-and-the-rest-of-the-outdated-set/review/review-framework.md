# Review framework — task 498

## Budget (by-diff)

- Lines changed: 152
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

**Date:** 2026-11-22
**Host task:** kanban/in-progress/498-bring-all-nu-get-pins-current-mediator-15-0-0-beta-10-state-12-0-0-beta-19-contracts-2-0-0-beta-34-and-the-rest-of-the-outdated-set/
**Diff scope:** round 2 re-review of `2df38b0bc` (parent `6a3524906`). Product delta: `.github/workflows/workflow.yml`, `skills/tw-deploy/SKILL.md`, and the dev-cli publish/deploy CLI floor. Round 1 reviewed the branch against `master` (effort 3, 1462 lines, turn cap 200) and stays in `review/round-1/`.
**Plan / brief:** Round 1 M1 (the release job installs the pinned Aspire CLI before `dev workflow`) and M2 (`dev publish` and `dev deploy` refuse a CLI below 17.0). `dev db nuke` stays on 13.6.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer-grok session 01a20280-df44-7a33-a806-7719c1fce04b (2026-11-22). Round 1 session 01a20272-ddfd-7171-87fc-672623d3891e.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
