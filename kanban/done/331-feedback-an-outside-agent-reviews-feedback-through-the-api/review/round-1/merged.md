# Round 1 — merged findings
**Date:** 2026-11-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/Directory.Build.props:14, Directory.Packages.props:38-40
- Description: The FastEndpoint generator, which is in the `TimeWarp.Architecture.Generators`
  platform package, changed, but `<Version>` and the pins stay at `2.0.0-beta.30`. Generated
  apps restore the generator at that pin, and it cannot read the contract `.xml`. Their OpenAPI
  document is missing the ceremony and list-feedback text, and the shipped tests
  `OpenApiAgentReviewerGuide_` `Issuance_…` and `ListFeedback_…` fail there. The Results do not
  record a `dev check-version` run, which AGENTS.md "Before a PR" requires.
- Suggestion: Run `dev check-version`. If beta.30 is already published, bump `<Version>` and the
  platform pins to the next beta in one commit (precedent c9a40020e). Record the outcome in
  Results.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/platform/identity-host/open-api-agent-reviewer-guide-server.cs:531
- Description: The scheme guide names steps by kebab id only. No code in this repo sets those
  ids as `operationId`, so a document-only agent cannot map a step to a path for certain. The
  doc test checks only string presence.
- Suggestion: Add method and path beside each id, derived from the contract `[ApiRoute]`. Assert
  each path exists under `paths` in the document test.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
