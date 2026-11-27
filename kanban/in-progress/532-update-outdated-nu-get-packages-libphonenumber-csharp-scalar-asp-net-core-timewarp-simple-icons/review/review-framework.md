# Review framework — task 532

**Date:** 2026-11-27
**Host task:** kanban/in-progress/532-update-outdated-nu-get-packages-libphonenumber-csharp-scalar-asp-net-core-timewarp-simple-icons/
**Diff scope:** branch `task/532-update-outdated-nu-get-packages-libphonenumber` vs `master` (`master...HEAD`). Product change is `Directory.Packages.props` in `24ed6b20e`. Kanban `task.md` moved to in-progress on this branch.
**Plan / brief:** Bump `libphonenumber-csharp` 9.0.44, `Scalar.AspNetCore` 3.3.0, and `timewarp-simple-icons` 16.38.1, and keep the Scalar comment in step with 3.3.0. Fix call sites only if the bumps break them. Do not touch `source/Directory.Build.props` `<Version>`. The walk also bumped `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` to 4.0.0-beta.18 after `package-pin-currency` failed.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer session d1237655-0403-44df-809b-dbb0618f0b09; reviewer session not reported (Grok 4.7)

## Budget (by-diff)

- Lines changed: 133
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
