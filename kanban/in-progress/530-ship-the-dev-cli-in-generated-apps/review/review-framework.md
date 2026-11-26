# Review framework — task 530

**Date:** 2026-11-26
**Host task:** kanban/in-progress/530-ship-the-dev-cli-in-generated-apps/
**Diff scope:** Round 3 reviews the branch vs `master` (the by-diff budget below). Product files are unchanged since `f88c6cd88`. Round 2 reviewed that fix delta only (`app-features.cs`, `deploy-command.cs`, `app-features-tests.cs`, `readme.md`). Round 1 reviewed the branch vs `master`.
**Plan / brief:** Ship `tools/dev-cli/` (and the root files it reads) in a generated app, drop template-only commands, and refuse or exclude commands whose feature flag is off. Round 3 re-checks that whole change, including the M1/M2 fix: a refusal names the flag that is actually off, and `dev deploy` refuses without web.
**Effort:** round 3 is 3 (general only), per the by-diff budget below. Round 1 was 3. Round 2 was 2.
**Reviewer roster:** general
**Session IDs:** round 1 grok `01a2149c-8f0e-7c20-a0c6-5ae91cc3abd7`; round 2 grok `01a214ad-d381-7e20-abcd-77224169cb95`; round 3 grok `01a21623-3f19-7df0-800e-0fd7f7bfe862`

## Budget (by-diff)

- Lines changed: 1749
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

## Record reviewed

Round 1: implementer `## Results` / `### How to validate` record `./bin/dev build`, targeted dev-cli tests, `./bin/dev template-smoke` and `--matrix` (six variants), `ganda repo audit`, and a manual packed-template pass over the flag-off refusals. That record covered the pack, the command list, and `dev build` through the generated CLI. It did not exercise `dev deploy migrate` on `--web false`. Gates were not re-run.

Round 2: the fix-pass record adds `./bin/dev build`, `./bin/dev test tests/tools/dev-cli-tests/app-features-tests.cs` (6/6), a manual refusal of `dev deploy migrate` in an empty git checkout (names web, exit 1) and of `dev deploy --no-browser` with only the yarp marker present (names web, exit 1), and `ganda repo audit` (passed). Template-smoke was not rerun; the fix does not change `template.json` or the generated layout. That record covers this delta. Gates were not re-run. One targeted Python check replayed the new marker-exclude rule against `.template.config/template.json` to see whether a flag outside `Requires` still removes a marker. It did not.

Round 3: the same record covers the branch. Commits after `f88c6cd88` only edit this kitchen. Gates were not re-run. No targeted command.
