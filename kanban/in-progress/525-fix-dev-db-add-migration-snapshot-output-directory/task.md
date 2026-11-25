# Fix dev db add-migration snapshot output directory

## Description

Running `dev db add-migration <Name>` (`tools/dev-cli/endpoints/db-add-migration-command.cs` and `tools/dev-cli/services/db-ef.cs`) places the new migration and designer files in `source/container-apps/web/platform/postgres/migrations/`, but writes the updated `PostgresDbContextModelSnapshot.cs` into the wrong folder:

`source/container-apps/web/projects/web-infrastructure/TimeWarp/Architecture/Persistence/Migrations/PostgresDbContextModelSnapshot.cs`

instead of updating the tracked snapshot at:

`source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`

This surfaced during task 520 (`AddFeedbackLastActivity`), where the implementer had to manually move the snapshot over the tracked file and delete the stray `TimeWarp/...` directory tree.

## Cause

`web-infrastructure.csproj` links `source/container-apps/web/platform/postgres/migrations/*.cs` from outside the project directory (`../../platform/postgres/migrations`), while `DbEf.BuildAddMigrationArguments` passes `--output-dir ../../platform/postgres/migrations --namespace TimeWarp.Architecture.Persistence.Migrations`. When EF Core's `MigrationsScaffolder` cannot resolve the existing snapshot path inside the project directory, it falls back to placing `PostgresDbContextModelSnapshot.cs` under `<ProjectDir>/<NamespacePath>/`.

## Scope

1. **Fix `dev db add-migration` so `PostgresDbContextModelSnapshot.cs` always lands in `source/container-apps/web/platform/postgres/migrations/`:**
   - First check whether EF Core's `dotnet ef migrations add` options or `web-infrastructure` project metadata can make EF Core write the snapshot directly to `--output-dir` without creating `TimeWarp/Architecture/Persistence/Migrations/`.
   - If EF Core's `MigrationsScaffolder` always emits the snapshot under the namespace path for linked outside-project migrations, add deterministic post-scaffold reconciliation in `DbEf` / `DbAddMigrationCommand` that detects the misplaced `PostgresDbContextModelSnapshot.cs`, moves it over the tracked `source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`, removes the empty generated directory segments under `web-infrastructure/`, and fails loudly if neither snapshot was updated.
   - Preserve `dotnet-new` `sourceName` template safety (no hardcoded unrewritten namespace strings outside `DbEf.MigrationsNamespace`).
2. **Add unit tests in `tests/tools/dev-cli-tests/db-ef-tests.cs`:**
   - Keep `DbEf` free of Amuru/Terminal dependencies so `dev-cli-tests` continues to compile-include it cleanly.
   - Cover the snapshot path resolution / relocation and empty-directory cleanup (using a temporary directory fixture), plus any argument changes.
3. **Update `#region Design`** in `tools/dev-cli/services/db-ef.cs` and `tools/dev-cli/endpoints/db-add-migration-command.cs` to record the behavior.

## Goal (implementer restatement)

`dev db add-migration <Name>` must leave the updated `PostgresDbContextModelSnapshot.cs` in
`source/container-apps/web/platform/postgres/migrations/` (beside the new migration and designer
files), and must leave no stray `TimeWarp/Architecture/Persistence/Migrations/` tree or duplicate
snapshot under `web-infrastructure/`.

EF Core's `MigrationsScaffolder.Save` finds the existing snapshot only by searching the project
directory, and the tracked migrations are compile-linked from outside it, so the snapshot always
falls back to `<ProjectDir>/<namespace path>/`. `--output-dir` only steers the migration and
designer files; there is no EF option for the snapshot. So the fix is a post-scaffold
reconciliation in `DbEf` that the command calls.

Acceptance:
- After a successful scaffold, a misplaced snapshot under the infrastructure project (outside
  bin/obj) is moved over the tracked snapshot, and the empty directories it leaves are removed up
  to (not including) the project directory.
- If no misplaced snapshot exists and the tracked snapshot content did not change, the command
  fails with a non-zero exit code and says so. More than one misplaced snapshot also fails.
- `DbEf` stays free of Amuru/Terminal; no new hardcoded namespace strings (sourceName safety).
- Unit tests in `tests/tools/dev-cli-tests/db-ef-tests.cs` cover relocation, cleanup, already-in-
  place, not-updated, and ambiguous cases with a temp-directory fixture.
- Design regions in `db-ef.cs` and `db-add-migration-command.cs` record the behavior.
- Gates: `./bin/dev build`, the dev-cli-tests DbEf classes, `ganda repo audit`.

## Checklist

- [x] Investigate root cause with `dotnet ef migrations add` and implement the cleanest fix in `tools/dev-cli/services/db-ef.cs` and `tools/dev-cli/endpoints/db-add-migration-command.cs`
- [x] Ensure no stray `TimeWarp/Architecture/Persistence/Migrations/` directory or duplicate `PostgresDbContextModelSnapshot.cs` remains under `source/container-apps/web/projects/web-infrastructure/`
- [x] Add unit tests in `tests/tools/dev-cli-tests/db-ef-tests.cs`
- [x] Verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, and `ganda repo audit`

## Session

- Created: 37533 (2026-11-25)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session ef5ca338-ed06-4a4c-9a8c-211ab6a5cc56, max-turns 500, Done — 2026-11-25T16:37:21Z
- Review oracle: Grok 4.7 (xAI), session 01a21391-c338-7db1-952c-987f0ceb4868, effort 2, roster general, turn cap 120, round 1 — 2026-11-25. Disposition clean.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a21391-c338-7db1-952c-987f0ceb4868, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-25T16:43:43Z

## Results

Root cause confirmed: EF Core's `MigrationsScaffolder.Save` honours `--output-dir` for the
migration and designer files, but locates the snapshot only by searching the project directory
(`TryGetProjectFile`). The tracked snapshot is compile-linked from outside `web-infrastructure/`,
so EF never finds it and writes to `<ProjectDir>/<namespace path>/`. `dotnet ef migrations add`
has no snapshot-directory option, so the fix is post-scaffold reconciliation.

- `tools/dev-cli/services/db-ef.cs`: new `MigrationsDirectory` and `SnapshotFileName` constants;
  `ReconcileSnapshot(projectDir, migrationsDir, trackedSnapshotBefore)` finds misplaced snapshots
  by file name under the project (skipping `bin/` and `obj/`), moves a single one over the tracked
  snapshot, deletes emptied directories up to the project directory, and returns `Relocated`,
  `AlreadyInPlace`, `NotUpdated`, or `Ambiguous`. No namespace-derived path, so no new
  namespace strings (sourceName safe). Still free of Amuru/Terminal. Design region updated.
- `tools/dev-cli/endpoints/db-add-migration-command.cs`: reads the tracked snapshot before the
  scaffold, calls `ReconcileSnapshot` after it, prints what moved and which directories were
  removed, and exits 1 on `NotUpdated` or `Ambiguous`. The success line uses
  `DbEf.MigrationsDirectory`. Design region updated.
- `tests/tools/dev-cli-tests/db-ef-tests.cs`: Purpose region added; `MigrationsDirectory` is
  checked against `InfrastructureProject` + `MigrationsOutputDir` (the two must agree); new
  `ReconcileSnapshot_Given_` class (8 tests) with a temp-directory fixture covering relocation and
  cleanup, cleanup stopping at a non-empty parent, first snapshot, in-place update, not updated,
  no snapshot, bin/obj ignored, and two misplaced copies.

### Gates

- `./bin/dev build`: succeeded, 0 warnings, 0 errors.
- `tests/tools/dev-cli-tests/db-ef-tests.cs`: not a standalone runfile (no `#if !JARIBU_MULTI`
  guard; it is part of the `dev-cli-tests` project). Ran `./bin/dev test --project dev-cli-tests`:
  413 passed, 0 failed.
- End-to-end: `./bin/dev db add-migration Task525Probe` (after `self-install`) printed the move of
  `web-infrastructure/TimeWarp/Architecture/Persistence/Migrations/PostgresDbContextModelSnapshot.cs`
  to `platform/postgres/migrations/` and the removal of the four empty directories; the tracked
  snapshot diff was only the `LastMigrationId` line, and no `TimeWarp/` tree remained. The probe
  migration files were deleted and the snapshot restored before commit.
- `ganda repo audit`: passed with 1 advisory warning (`vscode-window-icon`: local `.vscode/settings.json` missing). `ganda repo audit --fix` wrote that git-ignored local file; rerun passes all checks. No tracked change.

### How to validate

Smoke: from the task worktree, `dotnet run --file tools/dev-cli/dev.cs -- self-install`, then
`./bin/dev db add-migration ValidateProbe`, then `git status --short` and
`ls source/container-apps/web/projects/web-infrastructure/`.

Expect: the command prints `Moved .../web-infrastructure/TimeWarp/Architecture/Persistence/Migrations/PostgresDbContextModelSnapshot.cs to source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs.`
and four `Removed empty ...` lines, exits 0; `git status` shows the modified tracked snapshot
plus the two new `*_ValidateProbe*.cs` files in `platform/postgres/migrations/`, and
`web-infrastructure/` holds no `TimeWarp/` directory. Clean up with
`rm source/container-apps/web/platform/postgres/migrations/*_ValidateProbe*.cs` and
`git checkout -- source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`.
Unit: `./bin/dev test --project dev-cli-tests` passes, including `ReconcileSnapshot_Given_`.

### Review

- Rounds: 1. Roster: general. Effort: 2 (by-diff, 517 lines, turn cap 120).
- Counts: bug 0, suggestion 0, nit 0. Open 0. Fixed 0. Wontfix 0.
- Disposition: clean. The implementer restatement matches the original task (no misread). No escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Reviewer did not re-run build, test, or audit. Those stay with the gates recorded above, the host audit node, and CI.

## Review

- Round 1: Done

## Notes

- Observed during task 520 (`AddFeedbackLastActivity`).
- Existing tests live in `tests/tools/dev-cli-tests/db-ef-tests.cs`.
- Review kitchen: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md` (clean, 2026-11-25).
