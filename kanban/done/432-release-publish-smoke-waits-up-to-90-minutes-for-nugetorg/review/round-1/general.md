# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** branch `task/432-release-publish-smoke-waits-up-to-90-minutes-for-n` vs `master` merge base (product commit c3c952d29, `tools/dev-cli/endpoints/template-publish-smoke-command.cs`; plus kanban moves)

## Summary

The change replaces the hardcoded 12-minute flatcontainer deadline in `WaitForFlatcontainerAsync`
with `FlatcontainerWaitBudget` (90 minutes), declared next to `InstallRetryBudget`. The deadline,
the "budget N min" start line, and the timeout line all read that span. The poll delay cap (already
60 s) is now the named `FlatcontainerPollDelayCap`, so the doubling 5 s backoff still stops at one
minute. `InstallRetryBudget` is unchanged at 15 minutes. The Design region and the old "its 12 min
budget" comment were updated. Risk is low: the loop's behavior is unchanged except the deadline.

Verified against the repo:

- `.github/workflows/workflow.yml` has no `timeout-minutes` anywhere, so the release job runs under
  GitHub's 360-minute default. Results states this correctly.
- `documentation/developer/guides/releasing.md` and this repo's `skills/` do not state a 12-minute
  wait, and `tw-release` is not in this repo's `skills/`. No doc edit was needed.
- No literal `12` minute value remains in the command file.
- `dotnet run tools/dev-cli/dev.cs -- --help` exits 0 in this worktree (runfile compiles).
- Implementer restatement matches the task. No misread.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/endpoints/template-publish-smoke-command.cs:25
- Description: The Design region repeats the values "90 minutes" and "(1 minute)" next to the
  constant names, and says "InstallRetryBudget stays 15 minutes". Those literals can drift from the
  constants, which is the kind of disagreement the task set out to remove from the printed messages.
- Suggestion: Optional. Name the constants without restating their values, or accept as-is since the
  task asked the Design region to reflect the new span and the constant names sit beside the values.
- Status: open
