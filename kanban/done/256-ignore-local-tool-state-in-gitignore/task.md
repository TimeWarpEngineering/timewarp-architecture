# Ignore .local/ tool state in .gitignore

## Description

The origin-home checkout accumulates an untracked `.local/` folder (GitHub CLI XDG state,
`.local/state/gh/device-id`). `ganda pr merge`'s base sync now refuses to sync when any
non-kanban local change is present, so every merge ended with a manual fast-forward. Add
`.local/` to `.gitignore`.

## Checklist

- [x] `.local/` added to `.gitignore` with a one-line reason
- [x] `ganda repo audit` passes

## Results

Added `.local/` next to `.localhistory/` in `.gitignore`. Nothing under `.local/` was ever tracked.

## Notes

- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-29)
