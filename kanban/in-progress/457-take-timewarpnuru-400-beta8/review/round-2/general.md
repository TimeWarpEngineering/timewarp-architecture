# Round 2 — general
**Date:** 2026-10-30
**Scope reviewed:** branch `task/457-take-timewarpnuru-400-beta8` (HEAD `9e44b197e`) vs `master`;
product paths `Directory.Packages.props`, `tools/dev-cli/Directory.Build.props`. Re-checked against
the retargeted `## Requirements` (operator decision 2026-10-30: beta.9, drop RCS1194).

## Summary

No fix pass has run since round 1. The only commit after round 1 (`9e44b197e`) edits `task.md`:
it retargets the requirements to 4.0.0-beta.9 and asks for `RCS1194` to come out of the dev CLI
`NoWarn`. The product diff is the same as in round 1. Both pins are still `4.0.0-beta.8`, and
`RCS1194` and its comment are still in `tools/dev-cli/Directory.Build.props`. The upstream fix
exists: the local cache has `TimeWarp.Nuru.DevCli` 4.0.0-beta.9, and its
`content/any/services/package-feed-lookup.cs:142` declares
`PackageFeedUnreadableException(HttpRequestError, string?, Exception?, HttpStatusCode?)`. That is
the constructor RCS1194 asked for, so the fix pass can do the work in this repo.

The `## Goal (implementer restatement)` still targets beta.8. That matched the task when it was
written, before the retarget, so it is a stale restatement, not a misread. The fix pass should
refresh it.

## Issues

### Issue 1 — Severity: bug (carries round-1 M1)
- File: tools/dev-cli/Directory.Build.props:13
- Description: `RCS1194` is still in the dev CLI `NoWarn`, with its comment (lines 13–16). Its
  only effect is to make the warnings-as-errors runfile build pass, which breaks "Do not suppress
  warnings". The operator rejected keeping it as an exception (task.md `### Review`, Steve,
  2026-10-30). Silenced rule: Roslynator RCS1194.
- Suggestion: Remove `RCS1194` from the `NoWarn` list and delete the comment lines that explain
  it. Build `tools/dev-cli/dev.cs` on beta.9 and confirm it reports 0 warnings and 0 errors.
- Status: open

### Issue 2 — Severity: bug
- File: Directory.Packages.props:18
- Description: `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are pinned to `4.0.0-beta.8`. The
  requirements say "Bump both pins to `4.0.0-beta.9`". Issue 1 depends on this bump, because only
  beta.9 adds the constructor.
- Suggestion: Change both `PackageVersion` entries to `4.0.0-beta.9`. Do not use
  `VersionOverride`. Self-install `bin/dev` from the new pin. Then re-run the gates: `ganda repo audit`,
  `dev build`, `dev test`, `dev template-smoke`, and `dev check-version`.
- Status: open

### Issue 3 — Severity: suggestion
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md (`## Goal`, `## Results`, `## Checklist`)
- Description: The restatement, Results, and How to validate still describe beta.8 and the RCS1194
  suppression. They say "Both … are `4.0.0-beta.8`", "the dev CLI `NoWarn` list includes RCS1194",
  and "no RCS1194 error". Three checklist items are still unchecked: beta.9 pins, RCS1194 removed,
  and gates on beta.9.
- Suggestion: In the fix pass, rewrite these sections for beta.9 with no RCS1194 suppression, record
  the beta.9 gate results, and tick the checklist.
- Status: open
