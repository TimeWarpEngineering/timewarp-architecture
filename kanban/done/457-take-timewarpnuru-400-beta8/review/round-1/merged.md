# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tools/dev-cli/Directory.Build.props:17
- Description: The change adds `RCS1194` to the project-wide dev CLI `NoWarn` so the
  warnings-as-errors runfile build passes on `TimeWarp.Nuru.DevCli` 4.0.0-beta.8. The diagnostic
  fires on the sealed `PackageFeedUnreadableException` in package content, which this repo cannot
  edit. This conflicts with the task requirement "Do not suppress warnings". Silenced rule:
  Roslynator RCS1194.
- Suggestion: Operator decision. Either (a) accept it as a documented exception, the same as the
  existing package-content IDE ids, or (b) get a Nuru upstream fix (add the `HttpRequestError`
  constructor or mark the DevCli content generated), take that beta, and remove `RCS1194`. A fix
  pass on this id cannot resolve it without cross-repo work, which the spec does not authorize.
- Source: general
- Disposition notes: awaiting operator decision (accept as exception vs upstream Nuru fix)

## Duplicates / conflicts

- None (single reviewer).
