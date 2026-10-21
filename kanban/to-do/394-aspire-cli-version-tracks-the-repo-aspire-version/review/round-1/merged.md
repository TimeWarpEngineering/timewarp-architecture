# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/aspire-run.cs:266
- Description: The workflow agreement check passes whenever `<AspireVersion>` appears anywhere in
  `workflow.yml`. The comment at workflow.yml:227 contains it. So an `Aspire.Cli` install with no
  `--version`, or with a version from another source, still passes the check.
- Suggestion: Check the install command and the variable it uses. Require `--version "$version"` with
  `$version` set by the `sed` read of `Directory.Packages.props`, or ignore `#` comment lines. Add a
  test where the property appears only in a comment and the install has no `--version`. That test
  must fail.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/aspire-run.cs:158
- Description: A prerelease `<AspireVersion>` (for example `14.0.0-preview.1.26600.1`) does not parse,
  so every Aspire verb is refused, plain `dev run` included. The message says "Could not read
  <AspireVersion>" even though the property is present.
- Suggestion: Parse the numeric core before `-`/`+`, as `ParseCliVersion` does, or name the bad value
  in the message. Add a prerelease test.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
