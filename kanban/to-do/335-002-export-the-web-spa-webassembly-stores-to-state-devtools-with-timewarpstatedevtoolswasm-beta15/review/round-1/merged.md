# Round 1 — merged findings
**Date:** 2026-10-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tests/container-apps/web/web-spa-playwright-tests/state-dev-tools-wasm-playwright-tests.cs:482
- Description: The live wasm-span assertion runs only with a Debug web-spa build. `dev test` and CI
  build Release, so only the Release (nothing-leaks) branch is gated.
- Suggestion: Add a Debug lane for this test class to `dev test` or to the nightly workflow.
- Source: general
- Disposition notes: wontfix on this id (decided by the review oracle). The task requires the
  Debug-only package reference, so a Release build cannot observe a span. The Debug run is one
  scripted command in How to validate. The reviewer re-ran it and it passed (1/1, a `runtime=wasm`
  span). Adding a Debug build configuration to the dev-cli or CI test lanes changes CI cost and lane
  policy, so it is a maintainer decision outside this task's scope. Recommended as a follow-up task.

## Duplicates / conflicts

- None (single reviewer).
