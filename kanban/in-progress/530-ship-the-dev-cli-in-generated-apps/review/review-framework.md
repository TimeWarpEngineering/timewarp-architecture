# Review framework — task 530

**Date:** 2026-11-26
**Host task:** kanban/in-progress/530-ship-the-dev-cli-in-generated-apps/
**Diff scope:** Round 4 reviews the branch vs `master` (the by-diff budget below) after the origin/master merge. The new product delta is `AspireDeploy.ServesThroughIngress`, the yarp checks in `deploy-command.cs` and `open-command.cs`, the `(!postgres)` exclude of `open-api-binary-content-tests.cs`, and the `template-smoke-runner.cs` resolution (task 528 `--variant` / SmokeWebOnly kept beside the generated CLI checks). Round 3 reviewed the branch before that merge (product unchanged since `f88c6cd88`). Round 2 reviewed the M1/M2 fix delta. Round 1 reviewed the branch vs `master`.
**Plan / brief:** Ship `tools/dev-cli/` (and the root files it reads) in a generated app, drop template-only commands, and refuse or exclude commands whose feature flag is off. Round 4 re-checks M1 and M2 after the merge, and the task 527 adjustment: `dev open` and `dev deploy` refuse a missing yarp only on compose and kubernetes.
**Effort:** round 4 is 3 (general only), per the by-diff budget below. Round 1 was 3. Round 2 was 2. Round 3 was 3.
**Reviewer roster:** general
**Session IDs:** round 1 grok `01a2149c-8f0e-7c20-a0c6-5ae91cc3abd7`; round 2 grok `01a214ad-d381-7e20-abcd-77224169cb95`; round 3 grok `01a21623-3f19-7df0-800e-0fd7f7bfe862`; round 4 grok `01a216b5-ca35-7622-9303-ffeb97dea72c`

## Budget (by-diff)

- Lines changed: 1382
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

Round 4: the merge-conflict fix pass records `./bin/dev build`, `./bin/dev test` on `aspire-deploy-targets-given-tests.cs` and `app-features-tests.cs`, a `--matrix` run whose first five variants passed generated `dev build` (SmokeNoPostgres failed its solution build; the run then hit the harness time limit during SmokeWebOnly's generated-CLI step), the `(!postgres)` exclude, and successful `--variant` reruns of SmokeNoPostgres and SmokeWebOnly (each includes generated `dev build`). A manual `git init` checkout with only `web-server.csproj` refused compose `dev open` and `dev deploy` naming yarp, and `dev open --target aca` passed the feature check and stopped at the AppHost preflight. `ganda repo audit` passed. That record covers this delta. Gates were not re-run. No targeted command.
