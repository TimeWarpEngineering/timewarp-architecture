# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** framework scope (workflow.yml, template-smoke command + harness, AGENTS.md, tw-feature-placement skill text, task 376 kitchen)

## Summary

`dev template-smoke` gains `--matrix` (all `SmokeMatrix` variants) and `--app-tests` (generated
`tests/**` projects under the tier 3 MTP gate); the loop now reports every failing variant.
workflow.yml adds a `17 7 * * *` schedule and a `nightly` dispatch mode that runs
`--matrix --app-tests`, and `release` skips a nightly dispatch. Verified: `detect-paths`,
`ci-group`, `ci` and `skill-lint` are gated on push/pull_request, so a schedule runs only
template-smoke; `probe` and `merge` behavior is unchanged; cache key and smoke eviction are
untouched; `inputs.mode` interpolation is a choice input (no injection). Skill text is present
tense with no client names. Goal restatement matches the task (no misread). Low risk.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/endpoints/template-smoke-command.cs:177
- Description: default-only selection used `SmokeMatrix[..1]`, which silently depends on
  `SmokeDefault` staying the first entry; reordering the matrix would smoke a flag-off variant as
  the PR gate with no error.
- Suggestion: select the default by meaning (the entry with no extra template args) instead of
  by position.
- Status: open
