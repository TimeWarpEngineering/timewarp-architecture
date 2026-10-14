# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** same as framework (`deploy-migrate-command.cs`, `deploy-operate.cs`, `deploy-operate-tests.cs`, `skills/tw-deploy/SKILL.md`)

## Summary

The change compares source migration ids (file names under each sibling app's
`platform/postgres/migrations`) with the `__EFMigrationsHistory` insert ids in the published
script, warns before the plan/prompt, refuses under `--yes` unless `--allow-stale`, and qualifies
a successful report. All Requirements are met; aca reads the script published beside the bundle
and skips with a message when absent. Re-verified: dev-cli-tests 226/226 and `ganda repo audit`
pass in this worktree. Regexes are correct for EF output (`.Designer.cs` and the snapshot cannot
match the source pattern). Low risk.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs:280
- Description: `CheckStaleAsync` unions the ids of **every** sibling app's migrations folder but
  compares them with one script (`efmigrations/web-migrations.sql`). A generated app that adds a
  second postgres-backed app would always report its migrations as missing (a false stale, and
  `--yes` refusal).
- Suggestion: map each script to its app's folder, or compare only the folder of the app the
  script belongs to.
- Status: open
