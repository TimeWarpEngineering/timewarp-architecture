# Release publish smoke waits up to 90 minutes for nuget.org

## Description

The release run's last step, `dev template-publish-smoke`
(`tools/dev-cli/endpoints/template-publish-smoke-command.cs`), polls the nuget.org flatcontainer
until every published package lists the release version. Then it installs the template and builds
a generated app. The flatcontainer wait is hardcoded to 12 minutes
(`WaitForFlatcontainerAsync`: `TimeSpan.FromMinutes(12)` and a separate "budget 12 min" message
string).

Release v2.0.0-beta.28 (run 40728399501, 2026-10-28) pushed every package, but nuget.org took
about 15 minutes to list TimeWarp.Architecture and TimeWarp.Contracts. The step timed out and the
run went red. The same smoke passed when run by hand once the packages appeared. This has happened
on more than one release.

Raise the flatcontainer wait to 90 minutes. Steve approved 90 on 2026-10-28.

## Requirements

- One named `TimeSpan` constant for the flatcontainer budget, next to `InstallRetryBudget`, set
  to 90 minutes. Both the deadline and the "Waiting for ... (budget N min)" message use it, so the
  text cannot disagree with the value.
- Keep the polling backoff sensible for a 90-minute window. Cap the delay between attempts (for
  example, at a minute or two) so the step notices soon after the packages appear.
- Check `.github/workflows/workflow.yml` for a job or step `timeout-minutes` that would cut the
  release job off before 90 minutes plus the install and build. If there is one, raise it
  enough. If there is none, the GitHub default (360 minutes) is fine. Say which in Results.
- Leave `InstallRetryBudget` (15 minutes, after flatcontainer is satisfied) unchanged unless a
  reason turns up. If you change it, say why.
- Update the file's Design region, and the comment that mentions "its 12 min budget".
- Update `tw-release` or `documentation/developer/guides/releasing.md` only where it states the
  12-minute value.

## Out of scope

- Changing what the smoke test checks.
- Any change outside this repository. If something here seems to need one, stop and report.

## Checklist

- [x] Named 90-minute budget constant used by the deadline and the message
- [x] Poll delay capped for the longer window
- [x] Workflow job timeout checked (raised, or none found)
- [x] Design region, comments, and any doc stating 12 minutes updated
- [x] `dev build` 0/0; dev-cli runfile compiles (`dotnet run tools/dev-cli/dev.cs -- --help`)
- [x] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implementation: 01a183c5-00e6-7f72-8b5b-f434f7e51008 (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a183c5-00e6-7f72-8b5b-f434f7e51008, max-turns 500, Done — 2026-10-28T18:36:21Z

## Results

`FlatcontainerWaitBudget` is `TimeSpan.FromMinutes(90)` and sits next to `InstallRetryBudget`. `WaitForFlatcontainerAsync` adds that span to the deadline and prints it in the "budget N min" line and the timeout line, so those strings cannot name a different number.

The poll still starts at 5 seconds and doubles. `FlatcontainerPollDelayCap` stops it at 1 minute, so a 90-minute window still checks again within a minute of the packages appearing. `InstallRetryBudget` stays 15 minutes. Its loop was left alone.

`.github/workflows/workflow.yml` has no `timeout-minutes` on the `release` job or on any step. GitHub's default job limit is 360 minutes, which covers the 90-minute flatcontainer wait, the 15-minute install retry, and the generated-app build. The workflow file is unchanged.

`documentation/developer/guides/releasing.md` does not state a 12-minute wait. The `tw-release` skill lives in timewarp-flow and says flatcontainer indexes within minutes. It does not state 12 minutes, so it was not edited. No change outside this repository was required.

`dotnet run tools/dev-cli/dev.cs -- build` finished with 0 warnings and 0 errors. The client-name scan in that build passed. `dotnet run tools/dev-cli/dev.cs -- --help` exited 0, so the runfile compiled. `./bin/dev` was not used.

`ganda repo audit` exited 0 (34 passed, 1 warning, 2 skipped). The warning is `kanban-landed`: claims 070-001, 104-024, 104-025, 104-026, 123, 141, 198, and 244 have no usable worktree. Those claims are not this task. `kanban-landed --fix` does not remove that warning, so it was left as-is.

### How to validate

Smoke: From this worktree, run `dotnet run tools/dev-cli/dev.cs -- build` and `dotnet run tools/dev-cli/dev.cs -- --help`. In `tools/dev-cli/endpoints/template-publish-smoke-command.cs`, read `FlatcontainerWaitBudget`, the deadline, and the two budget lines in `WaitForFlatcontainerAsync`. Confirm the poll update uses `FlatcontainerPollDelayCap`. Search `.github/workflows/workflow.yml` for `timeout-minutes`.

Expect: The build reports 0 warnings and 0 errors. Help exits 0. `FlatcontainerWaitBudget` is 90 minutes and is what the deadline and both messages use. `FlatcontainerPollDelayCap` is 1 minute. `InstallRetryBudget` is still 15 minutes. `workflow.yml` has no `timeout-minutes`.

## Goal (implementer restatement)

The release smoke step gives nuget.org a fixed 12 minutes to list every package that was just pushed, then fails even when the packages show up a few minutes later. I will make that wait one named 90-minute span, and use that same span for the deadline and the budget line the step prints. The pause between polls stays capped so a long wait still checks again within about a minute of the packages appearing. I will look at the release job in `.github/workflows/workflow.yml` and raise `timeout-minutes` only if a limit there would kill the job before the 90-minute wait plus the install and the generated-app build. The 15-minute install retry after the listing check stays as it is. Comments and docs that still say the wait is 12 minutes get updated to the new span.

Acceptance:

- `FlatcontainerWaitBudget` (90 minutes) sits next to `InstallRetryBudget` and is what both the deadline and the "budget N min" line use.
- The flatcontainer poll delay still doubles from 5 seconds and stops growing at one minute.
- The workflow file either has no `timeout-minutes` on the release job (GitHub's 360-minute default is enough) or that limit is raised so it covers the wait, the install, and the build.
- The Design region, the "12 min budget" comment, and any `tw-release` or `releasing.md` sentence that states 12 minutes match the new span.
- `InstallRetryBudget` stays 15 minutes.
- `dev build` is 0 warnings and 0 errors, and `dotnet run tools/dev-cli/dev.cs -- --help` compiles the runfile.
- No client name is added to source, tests, or skills.

## Notes

- `bin/dev` is an AOT snapshot. Gate the change through the runfile, not a stale `./bin/dev`.
