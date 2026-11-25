# Review framework — task 526

**Date:** 2026-11-25
**Host task:** kanban/in-progress/526-build-unlisted-jaribu-aggregators-in-dev-build-and-isolate-validator-options-global-in-web-application-host/
**Diff scope:** branch `task/526-build-unlisted-jaribu-aggregators-in-dev-build-and` vs `master` (`master...HEAD`, 802 lines). Product commits: `328ea9738` (dev build + ValidatorOptions guard) and `cd2618de9` (runfile executable bit).
**Plan / brief:** `task.md` — `dev build` also builds tests projects the `.slnx` does not list, and `WebApplicationHost` restores FluentValidation `ValidatorOptions.Global` resolvers when the last in-proc host goes away.
**Effort:** 3 (by-diff, 802 lines)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a21338-bc62-7530-bc4f-8f8c648fb47a` (2026-11-25). Implementer session is on `task.md`.

## Budget (by-diff)

- Lines changed: 802
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
