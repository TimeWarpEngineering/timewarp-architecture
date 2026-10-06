# Round 1 — general
**Date:** 2026-10-06
**Scope reviewed:** full branch diff vs master (28 files)

## Summary

The change bumps four CPM pins and moves every Nuru handler in `tools/dev-cli` and
`tools/agent-identity-cli` from `ValueTask<Unit>` to `Task<Unit>`, replacing
`global using static TimeWarp.Nuru.Unit` with `global using TimeWarp.Mediator`. That removes the
CS0229 `Task` ambiguity. Outside those mechanical edits, the only changes are
`Task.FromResult(Unit.Value)` in keygen and the `RunStepAsync(string, Task<Unit>)` parameter in
workflow-command. Both keep the old semantics: the step is started eagerly and then awaited.
Risk is low.

Verified against the repo:
- No `ValueTask<Unit>` or `TimeWarp.Nuru.Unit` remains in `tools/`, `tests/` or `.githooks/`.
- Versions live only in CPM. The `.githooks` `#:package` directives are unversioned, and
  `timewarp-templates/` has no Nuru or Amuru pins.
- Neither global-usings Purpose region described the old using.
- Gates re-run in this worktree: dev-cli-tests 102/102, agent-identity-cli-tests 11/11, and
  `ganda repo audit` passes. One advisory memsearch-scaffold warning remains. It is
  pre-existing and caused by the installed ganda version.
- The Amuru 2.0.0-beta.1 pin is not required by the Nuru beta.79 nuspec, but it is the forward
  value that the audit's `--fix` writes. That fits the forward-only pin policy.

## Issues

None.
