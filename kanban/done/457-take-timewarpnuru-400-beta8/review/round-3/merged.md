# Round 3 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/Directory.Build.props:13
- Description: `RCS1194` in the dev CLI `NoWarn`.
- Source: general (carried from round 1 and round 2)
- Disposition notes: Fix pass `3a9888364` removes `RCS1194` and its comment. DevCli beta.9 has the
  `HttpRequestError` constructor. The runfile builds clean.

### M2 — Severity: bug — Status: fixed
- File: Directory.Packages.props:18
- Description: The pins were at beta.8, and the task needs beta.9.
- Source: general (carried from round 2)
- Disposition notes: Both `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are `4.0.0-beta.9`. There is no
  `VersionOverride`.

### M3 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md
- Description: Goal, Results, and Checklist described beta.8.
- Source: general (carried from round 2)
- Disposition notes: Commit `80fb4833a` rewrites Goal, Results, How to validate, and Checklist for beta.9.

### M4 — Severity: bug — Status: open
- File: tools/dev-cli/Directory.Build.targets:7
- Description: `OmitPackagedRepoConfigService` removes the packaged `RepoConfigService` whatever the
  DevCli version. The rule "delete the copy when the pin uses the generic overload" exists only in
  comments, so a later pin bump silently drops upstream behavior changes. This breaks the AGENTS.md
  hard rule "generate one from the other or add a build check".
- Suggestion: Guard the Compile removal to `4.0.0-beta.9`, using the item path or the `PackageVersion`.
  On any other pin, CS0101 then forces someone to revisit the copy.
- Source: general
- Disposition notes: Moot if the operator chooses option (b) on M5.

### M5 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/repo-config-service.cs:1
- Description: This is a local copy of first-party TimeWarp.Nuru.DevCli internals that works around
  CA2263 in beta.9 content. Nuru master `edaa9982` still calls `GetTypeInfo(typeof(RepoConfig))`, and
  no upstream task was found. In round 1 the operator chose the upstream fix over a local workaround.
- Suggestion: Operator decision. Either (a) accept the copy as a temporary exception, with M4's guard
  and an upstream Nuru task, or (b) wait for a Nuru release that uses `GetTypeInfo<RepoConfig>()`, then
  delete the copy and the target.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). M1–M3 keep their IDs from round 2.
