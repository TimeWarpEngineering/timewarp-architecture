# Round 2 — general (re-verify)
**Date:** 2026-10-13
**Scope reviewed:** fix delta on round-1 M1–M3 (uncommitted review-fix commit on the task branch)

## Summary

The review oracle re-verified the round-1 fixes against the post-fix diff:
- **M1:** the HTTP no-PII test now uses the reachable Operator + `admin.principals.read` shape, and the original Operator permissions and principal roles are restored in `finally`. The pre-existing 182-002 Administrator-shrink test is unchanged.
- **M2:** the Design regions now record the ASCII-only equivalence.
- **M3:** non-404 failures set a generic LoadError when no principal is loaded.

`dev build` reports 0 warnings and 0 errors, and `dev test -q` completed successfully. The fix delta introduces no new defects.

## Issues

<!-- none -->
