# Commit peacock statusBar.inactiveBackground in .vscode/settings.json

## Description

Local Peacock color edit (`statusBar.inactiveBackground`) was left uncommitted in the origin-home
checkout, so `ganda pr merge`'s base sync refused on the tracked modification after every merge.
Commit it (same shape as PR #393).

## Checklist

- [x] `.vscode/settings.json` gains `statusBar.inactiveBackground: #83f8e4`

## Results

Committed the one-line Peacock setting; origin-home is clean after sync.

## Notes

- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-29)
