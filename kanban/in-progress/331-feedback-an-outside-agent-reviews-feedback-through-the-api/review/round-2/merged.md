# Round 2 — merged findings
**Date:** 2026-11-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/Directory.Build.props:14, Directory.Packages.props:38-40
- Description (round 1): the generator changed with no version bump, and `dev check-version` was
  not recorded.
- Disposition notes: `./bin/dev check-version` exit 0 (re-run in round 2): source
  `2.0.0-beta.30` is ahead of NuGet `2.0.0-beta.29`, so beta.30 is unpublished and carries this
  generator. No bump is needed. Results record the run and the package list.
- Source: general

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/identity-host/open-api-agent-reviewer-guide-server.cs:78-97
- Description (round 1): the guide named steps by kebab id only, with no method or path.
- Disposition notes: fixed in 37f0bb8e5. `Step<TRequest>` appends `(METHOD /path)` from the
  contract's `GetHttpVerb()` and `GetRoute()`. The document test asserts each path is under
  `paths` with that method. `OpenApiAgentReviewerGuide_` 3/3 in round 2.
- Source: general

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
