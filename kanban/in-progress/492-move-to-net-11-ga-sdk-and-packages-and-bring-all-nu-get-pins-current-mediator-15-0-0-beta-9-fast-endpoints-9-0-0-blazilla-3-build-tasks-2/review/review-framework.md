# Review framework — task 492

**Date:** 2026-11-16
**Host task:** kanban/in-progress/492-move-to-net-11-ga-sdk-and-packages-and-bring-all-nu-get-pins-current-mediator-15-0-0-beta-9-fast-endpoints-9-0-0-blazilla-3-build-tasks-2/
**Diff scope:** branch `task/492-move-to-net-11-ga-sdk-and-packages-and-bring-all` vs merge-base `7d832296c743eed79532c7205655a7e17e71e671` (commits `efb95145a`..`d57de9b6a`; 38 files, 657 lines). Local `master` is one kanban-land commit ahead (`85af96d6d`, task.md rename only).
**Plan / brief:** task.md Description, Requirements, and `## Goal (implementer restatement)`: .NET 11 GA SDK `11.0.100`, current NuGet pins, majors called out, gates recorded in `## Results`.
**Effort:** round 1: 2 (general). Round 2: 1 (general). The budget block below is the round-2 host recording.
**Reviewer roster:** general
**Session IDs:** round 1 review oracle grok `01a1e404-05dd-76b3-8f24-4c107a376806` (2026-11-16). Round 2 review oracle grok `01a1e417-6866-79f2-af79-626ad1ea39d0` (2026-11-16). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 127
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

Round 1 values this block replaced: 657 lines, effort 2, turn cap 120. Round 1 scope stays in the header above and in `review/round-1/`.

## Round 2

**Date:** 2026-11-16
**Diff scope:** `15da1a135` since round-1 base `d57de9b6a64813333955adf0f255f59b6b160721`. Product delta is the FastEndpoints pin comment in `Directory.Packages.props` and the two OpenApi comments in `source/foundation/foundation-server/foundation-server.csproj`. Re-verify M1 on that delta.
**Effort:** 1 (general only; budget block above)
**Session IDs:** grok `01a1e417-6866-79f2-af79-626ad1ea39d0`, max-turns 80

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
