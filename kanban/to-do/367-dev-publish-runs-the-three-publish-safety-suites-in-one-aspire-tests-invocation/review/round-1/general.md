# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** same as framework

## Summary

The change extracts publish targets into a pure `AspirePublishTargets` (compile-included in
dev-cli-tests), generalises `AspirePublish.PublishAndCheckAsync` to N targets with one
`dotnet test` run, moves helm lint into a shared `HelmLintAsync`, and adds `dev publish all
[--no-build]`, which `dev workflow` now calls after its Release build. Behaviour of the
single-target commands is preserved (own class filter, builds aspire-tests, helm lint for
kubernetes). Risk is low.

Verified:
- aspire-tests is in `timewarp-architecture.slnx` and `dev build` builds Release, so
  `--no-build --no-restore` in `dev workflow` reads a fresh Release output.
- Only three classes in aspire-tests contain `Publish_Given_`; a dev-cli test pins that the
  combined filter selects exactly them.
- All `PublishAndCheckAsync` / old `Target` callers were updated; no other `AspirePublish.Fail`
  callers depend on the old `string?` return type.
- dev-cli-tests re-run in this worktree: 239/239 passed.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/services/aspire-publish.cs:130
- Description: `Fail` now returns `IReadOnlyList<string>?` so `PublishAndCheckAsync` can
  `return Fail(...)`; `PublishAsync` and `HelmLintAsync` call it and discard the result. The
  general helper's return type is shaped by one caller.
- Suggestion: make `Fail` `void` and write `{ Fail(...); return null; }` in `PublishAndCheckAsync`.
- Status: open
