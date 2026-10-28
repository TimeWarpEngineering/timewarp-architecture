# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tools/dev-cli/endpoints/template-publish-smoke-command.cs:25
- Description: Design region restates "90 minutes", "1 minute", and "15 minutes" beside the
  constant names; the literals could drift from the constants.
- Suggestion: Name the constants without their values.
- Source: general
- Disposition notes: wontfix (review oracle). The task required the Design region to describe the
  new span, each value sits next to the constant that owns it, and the runtime messages, which are
  what an operator reads, are derived from the constants. Not worth a fix pass on its own.

## Duplicates / conflicts

- None (single reviewer).
