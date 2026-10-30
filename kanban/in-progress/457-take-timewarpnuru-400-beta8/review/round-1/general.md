# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** branch vs master, product paths `Directory.Packages.props` and `tools/dev-cli/Directory.Build.props`

## Summary

The change moves both central pins, `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli`, from
`4.0.0-beta.7` to `4.0.0-beta.8`. It has no `VersionOverride` and no other pin changes. That part
matches the task and the implementer restatement. The restatement is accurate, so there is no
misread.

The only other product change adds `RCS1194` to the dev CLI `NoWarn` list. The task says
"Do not suppress warnings" and the restatement says "Do not add a suppression to hide a new
diagnostic". I re-verified the implementer's facts:

- `PackageFeedUnreadableException` is `public sealed` in
  `~/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.8/content/any/services/package-feed-lookup.cs:121`.
  It has the four classic constructors and not the `HttpRequestError` one.
- The dev CLI defines no exception types of its own today, so the project-wide `NoWarn` hides
  nothing in this repo's files right now.
- `dotnet build tools/dev-cli/dev.cs` builds on beta.8.

The fix the analyzer wants is in Nuru package content, which this repo cannot edit. Doing it
upstream is cross-repo work that this spec does not authorize.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/Directory.Build.props:17
- Description: The change adds `RCS1194` (Roslynator "implement exception constructors") to the
  project-wide dev CLI `NoWarn` so the beta.8 runfile builds. The diagnostic is real. It fires on
  `PackageFeedUnreadableException` in `TimeWarp.Nuru.DevCli` 4.0.0-beta.8 package content. The
  suppression is what makes the warnings-as-errors build pass, and it conflicts with the task
  requirement "Do not suppress warnings". Because it is project-wide, it would also hide RCS1194
  for any exception type the dev CLI adds later. The block already suppresses other ids for the
  same package-content reason (IDE0005 … IDE0290, documented), so the pattern is consistent. It
  is still a new suppression added to pass a gate.
- Suggestion: This needs an operator decision. This repo has no in-scope fix:
  (a) Accept it as a documented exception, consistent with the existing package-content ids
  (wontfix, operator-decided). Or
  (b) Have Nuru fix it upstream: add the `HttpRequestException(HttpRequestError, string,
  Exception?, HttpStatusCode?)` constructor, or mark the DevCli content as generated. Then take
  that beta and drop `RCS1194` from this list. Per standing policy, Nuru releases itself and this
  spec does not authorize cross-repo work.
  A narrower in-repo scope, such as a build-generated global analyzer config keyed to
  `$(NuGetPackageRoot)`, might be possible. It would be more machinery than the existing
  precedent uses.
- Status: open
