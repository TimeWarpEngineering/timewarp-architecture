# Round 2 — general
**Date:** 2026-09-12
**Scope reviewed:** re-verify M1 plus fix delta vs origin/master and working tree

## Summary

M1 is fixed: `create-role-tests.cs` now cites the live `tests/Directory.Build.props` Jaribu CA1707 allowlist, and `source/` plus `tests/` contain no remaining `foundation-domain-jaribu-tests` path references (kanban history only). The fix delta is a one-line comment retarget in the working tree; it introduces no new defects and does not disturb the original cleanup.

## Prior findings

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:29-30
- Notes: Working-tree diff replaces `tests/foundation/foundation-domain-jaribu-tests/Directory.Build.props` with `tests/Directory.Build.props`. That props file exists and carries `NoWarn` for CA1707 (Jaribu underscore naming). Grep over `source/` and `tests/` finds zero `foundation-domain-jaribu-tests` hits.

## Issues

<!-- new findings only, if any -->
