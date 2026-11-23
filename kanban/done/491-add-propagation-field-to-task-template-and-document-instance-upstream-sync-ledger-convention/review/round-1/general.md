# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** same as framework (branch vs master merge base)

## Summary

The change adds `## Propagation` to `kanban/task-template.md`, writes the instance upstream-ledger
convention in a new `kanban/readme.md`, points `kanban/overview.md` at that page, and ships the page
in the board skeleton. The pack csproj lists it explicitly, and `AssertKanbanSkeletonShipped` expects
it. All four acceptance criteria in the restatement are met. The restatement matches the original
task: checklist item 2 allowed `kanban/readme.md` when no repo skill owns kanban, and `skills/` has
no kanban skill. Shipping the page to generated apps is an addition, and it is sound, because the
ledger lives in the instance repo. Risk is low. The diff is docs, one MSBuild include, and a
one-entry change to a smoke allow-list.

Checked:

- The skeleton assert is exact both ways: every expected file must exist, and any other file or
  directory under `kanban/` fails. Adding `readme.md` to `expectedFiles` is enough. The success
  line and the Design region were updated to match.
- The csproj `Content Include` uses `%(FileName)%(Extension)`, so the file lands at
  `kanban/readme.md` in the pack. `.template.config/` has no kanban or readme exclude that could
  drop it.
- The example ledger uses `shop#031`, not the client name from the task Notes. The page ships to
  generated apps, and `dev check-client-names` skips `kanban/`, so this matters. Grepping the
  three shipped kanban files with `documentation/developer/client-names.txt` returns no hits.
- The `ganda kanban upstream status` command exists (`--origin`, `--json`, reads
  `kanban/upstream/*.md`), which matches what the page says about it.
- The recorded gates cover the change: `./bin/dev build` 0/0, default `template-smoke` succeeded
  with the new skeleton line, and `ganda repo audit` exit 0. No `#if` flag or template symbol
  changed, so `--matrix` is not needed. No `skills/` change, so no `dev lint-skills` run is needed.

Targeted command run: one read-only `rg -f documentation/developer/client-names.txt` over
`kanban/readme.md`, `kanban/task-template.md`, and `kanban/overview.md`. This checks whether a
client name leaks into shipped files that the client-names gate does not scan. Result: no match.

## Issues

### Issue 1 — Severity: nit
- File: kanban/task-template.md:10
- Description: `kanban/task-template.md` ships to every generated app, so instance authors see
  `## Propagation` and "Pick one word and delete the other two" on their own tasks. The field means
  something only on tasks in this template. The linked `readme.md` says "On a task in this
  template", but the template line itself does not say so. An instance author may fill it in with
  no effect.
- Suggestion: optionally add a few words, such as "(template repo tasks only; an instance may
  delete this section)". Leaving it as is also works, because the readme link explains the scope.
- Status: open
