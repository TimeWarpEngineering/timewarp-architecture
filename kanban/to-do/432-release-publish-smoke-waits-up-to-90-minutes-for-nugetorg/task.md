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

- [ ] Named 90-minute budget constant used by the deadline and the message
- [ ] Poll delay capped for the longer window
- [ ] Workflow job timeout checked (raised, or none found)
- [ ] Design region, comments, and any doc stating 12 minutes updated
- [ ] `dev build` 0/0; dev-cli runfile compiles (`dotnet run tools/dev-cli/dev.cs -- --help`)
- [ ] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)

## Notes

- `bin/dev` is an AOT snapshot. Gate the change through the runfile, not a stale `./bin/dev`.
