# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** `.githooks/pre-push.cs` diff (commit 219873fa) + task.md Results

## Summary

Purely additive baseline refresh: collects `refs/heads/task/*` → `refs/heads/{master,main}` pairs
from stdin and refuses them before the existing HEAD-is-home checks; raw-sha sources fall through.
Reuses the existing `IsHomeBranchDest` helper; header comment updated to describe the guard.
Re-verified: `ganda repo audit` passes all checks; stdin smoke — task/x→master exits 1 with the
guidance message, raw sha→master exits 0. No issues found.

## Issues

<!-- none -->
