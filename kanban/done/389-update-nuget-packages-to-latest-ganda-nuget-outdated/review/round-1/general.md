# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch vs master, all four product files, plus stale-reference sweep of the repo.

## Summary

The change is pin-only: 29 central pins moved forward, Microsoft.OpenApi stays on 2.12.2 with
nuspec evidence, TimeWarp.Nuru / DevCli move to 4.0.0-beta.2 for the audit nuru check, dotnet-ef
follows EF Core RC2, and `<Version>` plus all twelve platform pins move to 2.0.0-beta.22 in one
commit (2d533fcd2). No TimeWarp package was pinned backward. Risk is low. The only issues are
comment placement and one stale comment.

Gates re-run by the reviewer in this worktree:

- `./bin/dev check-version`: source 2.0.0-beta.22, NuGet 2.0.0-beta.21, "Version in source is new".
- `ganda repo audit`: "Repository passes all audit checks."
- `./bin/dev build`: first run hit a transient MSB4018 task crash in
  `timewarp-architecture-convention-analyzers.csproj` (about 150 dotnet processes were running on
  the machine). The rerun and a third run were 0 Warning(s), 0 Error(s). Not attributed to this diff.
- `./bin/dev test --no-build`: 23 of 23 projects passed, "Tests completed successfully!".
- `dev template-smoke` was not re-run. `artifacts/template-smoke/work/SmokeDefault/Directory.Packages.props`
  carries the new Nuru 4.0.0-beta.2 pin, so the smoke output post-dates the change.

Checked and not raised:

- Microsoft.Extensions.AI.OpenAI 10.11.0 resolves OpenAI 2.14.0 in web-server `project.assets.json`.
  Build and tests pass.
- EF migration Designer and snapshot files keep `ProductVersion` 11.0.0-rc.1.26425.128. That is
  normal: EF rewrites the annotation on the next `migrations add`, and it does not count as a
  pending model change.
- Runfile `#:package` directives carry no Nuru version, so CPM is the single version site.

## Issues

### Issue 1 — Severity: suggestion
- File: Directory.Packages.props:152
- Description: The rationale for TimeWarp.Nuru / TimeWarp.Nuru.DevCli 4.0.0-beta.2 was appended
  to the Microsoft.OpenApi / .NET 11 RC train comment block above the `Microsoft.OpenApi` pin.
  The Nuru pins are at lines 12–13 in the "TimeWarp Packages" group. A maintainer reading the
  Nuru pin finds no reason for the prerelease. A maintainer reading the OpenApi pin finds an
  unrelated Nuru note.
- Suggestion: Move the Nuru sentence into a comment directly above the Nuru pins at line 12, and
  remove it from the OpenApi block.
- Status: open

### Issue 2 — Severity: nit
- File: Directory.Packages.props:165
- Description: The Microsoft.Extensions.AI.OpenAI comment now says the pin is 10.11.0 but still
  warns that "adapter 10.10.0 TypeLoadExceptions on tool calls when OpenAI 2.14 is resolved".
  The adapter is 10.11.0 and OpenAI 2.14.0 is resolved. The warning names a version this repo no
  longer uses, so a reader cannot tell whether it still applies.
- Suggestion: Restate the Responses API warning without the old adapter version, or say that it
  was observed on 10.10.0 and has not been re-checked on 10.11.0.
- Status: open
