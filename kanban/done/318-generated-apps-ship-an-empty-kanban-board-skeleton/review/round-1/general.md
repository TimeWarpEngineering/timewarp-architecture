# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** commit bc61ab468 vs master

## Summary

The template csproj packs the five column `.gitkeep` files, `overview.md` and `task-template.md` as explicit
`Content` items, with no `kanban\**` glob. Tasks, journals and `task-examples/` cannot leak into the pack.
`AssertKanbanSkeletonShipped` checks both directions: every expected file must exist, and nothing else may
sit under `kanban/`. The overview is public-safe: no history, no task numbers, no client names. It names
ganda only as "where available". Every skill it cites exists under `skills/`. I re-ran the gate:
`dev template-smoke` passed, the skeleton check printed for all three matrix apps, and SmokeDefault/kanban
holds exactly the skeleton. Risk is low.

## Issues

### Issue 1 — Severity: nit
- File: kanban/task-template.md:49
- Description: The Smoke example says `./bin/dev run`. Generated apps do not ship `tools/dev-cli`, so the
  shipped template points to a command that doesn't exist.
- Suggestion: Use a command that works in a generated app, e.g. `dotnet run --project …/aspire-app-host`.
- Status: fixed
