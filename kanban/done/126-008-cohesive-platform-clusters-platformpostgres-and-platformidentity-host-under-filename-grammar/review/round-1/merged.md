# Round 1 — merged findings
**Date:** 2026-07-27
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/.../feature-filename-grammar-analyzer-tests.cs (SSOT drift)
- Description: Drift test did not lock WebPlatformTreeRoot / platform membership scan.
- Source: general
- Disposition notes: Extended props + membership assertions; analyzer tests 95/95 pass.
