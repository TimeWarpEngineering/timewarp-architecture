# Round 1 — general
**Date:** 2026-09-02
**Scope reviewed:** branch `task/208-ignore-routine-journals-and-untrack-committed-task` vs `origin/master` — product `.gitignore` routine-journal block; index deletes of the two tracked `task-work.journal.json` paths; kitchen `task.md` to-do → in-progress move

## Summary

The change appends the org routine-journal basename block to root `.gitignore` and removes the two `task-work.journal.json` paths that PR #318 left tracked on master. Risk is low: pure ignore + index deletes, no product `task.md` loss, and the leftover empty `kanban/in-progress/207-…/` directory is gone while `kanban/done/207-…/task.md` remains. All twelve re-verification claims passed; the template packaging already ships root `.gitignore`, and there is no product basename collision with the new ignore lines.

## Issues

