# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/525-fix-dev-db-add-migration-snapshot-output-directory` vs `master` (merge-base): `tools/dev-cli/services/db-ef.cs`, `tools/dev-cli/endpoints/db-add-migration-command.cs`, `tests/tools/dev-cli-tests/db-ef-tests.cs`, and the task restatement against the original brief. Gates were not re-run.

## Summary

`DbEf.ReconcileSnapshot` runs after a successful `dotnet ef migrations add`. It finds `PostgresDbContextModelSnapshot.cs` under the web-infrastructure project (skipping `bin/` and `obj/`), moves the single hit over the tracked snapshot in `source/container-apps/web/platform/postgres/migrations/`, and deletes directories left empty up to, but not including, the project directory. Zero or more than one misplaced snapshot fails the command with exit code 1 when the tracked file was not updated. The implementer restatement matches the original task: the EF-option investigation is recorded as a conclusion (no snapshot-directory switch; post-scaffold move), not a narrower goal. `DbEf` stays free of Amuru and Terminal, and the new code adds no namespace literal beyond the existing `MigrationsNamespace`. The recorded `## Results` cover build, `dev-cli-tests` (413 passed), audit, and an end-to-end probe that saw the move and the four empty directories removed. No issues.

## Issues

None.
