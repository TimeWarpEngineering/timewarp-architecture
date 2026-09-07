# Disposition — task 205-004

**Date:** 2026-09-07
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 found that `@bind-SelectedItems` cannot fix closed FluentCombobox labels: `FluentSelect.Initialize` only writes `GetOptionText` when `Value is TOption`. The fix on this task binds Language/Region as `TOption == TValue == string` with `OptionText` → `ProfileCatalog.LabelFor`. Round 2 re-verified M1/M2 as fixed and raised no new issues. Locked constraints hold (`FreeOption` unset, Theme `FluentSelect`, no mock auth, `SetIsoCulture` stays `en-US`). Catalog tests remain lookup coverage; live `/Profile` closed-field proof still needs Aspire on this branch (running AppHost is origin-home/master).

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
