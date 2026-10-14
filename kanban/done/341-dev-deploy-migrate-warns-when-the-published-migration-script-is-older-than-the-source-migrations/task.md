# `dev deploy migrate` warns when the published migration script is older than the source migrations

## Problem (2026-10-13)

After 339 merged, the cockpit ran `dev deploy migrate --target kubernetes` against the kind
deployment. It printed:

```
Up to date: no pending migrations (10 applied, latest 20260923180147_AddCredentialAccountHint)
```

That was true only relative to the **published script**
(`artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql`), which came from an older
`dev publish kubernetes`. The source already had three newer migrations under
`source/container-apps/web/platform/postgres/migrations/`:
- `20261010075816_AddFeedbackAttachments`
- `20261012115946_AddFeedbackTriage`
- `20261013120000_AddAdminProfilesReadPermission`

"Up to date" misled. The operator gets no hint that the published output is stale.

## Requirements

- Before running, compare the migration ids **in the source** with the ids **in the published
  script**:
  - source: the `migrations/` folder's migration file names (`<timestamp>_<Name>.cs`), excluding
    `*.Designer.cs` and the model snapshot. Find the folder from the AppHost or the postgres
    platform path; don't hard-code a single app;
  - script: the ids EF writes in each migration's `__EFMigrationsHistory` insert (the
    `'<MigrationId>'` literal), a stable part of EF's idempotent script output.

  If the source has ids the script lacks, print a **warning** before the confirmation prompt:

  ```
  ⚠ Published script is stale: source has 3 migration(s) the script does not
    (20261010075816_AddFeedbackAttachments, …). Run `dev publish <target>` first,
    then redeploy so the app and schema match.
  ```

  - Default: warn and continue only if confirmed. With `--yes`, **refuse** (exit non-zero) unless
    `--allow-stale` is passed, so unattended runs never silently apply an old script. Record the
    choice.
  - The ACA bundle path: apply the same comparison using the bundle's migrations if they're
    discoverable without reconstructing EF internals. Otherwise say the check isn't available for
    the bundle and skip it; record that.
- After the run, the "Up to date" / "Applied" summary says **"relative to the published script"**
  when the stale warning was shown.
- Generated apps without the `postgres` flag (no migrations folder) skip the check cleanly.
- **Tests** (`tests/tools/dev-cli-tests`): id extraction from a sample script and a sample folder
  listing, stale vs current, `--yes` refusal vs `--allow-stale`, no-migrations-folder skip.
- **Docs:** the `tw-deploy` migrate section mentions the stale check and the publish-then-deploy
  order.

## Checklist

- [x] Source vs script migration id comparison; warning text
- [x] `--yes` refuses on stale unless `--allow-stale`; summary qualifier
- [x] ACA bundle handling decision recorded
- [x] Tests + skill note
- [x] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`

## Results

- `dev deploy migrate` compares the source migration ids with the published script's before the plan.
  - Source: `<app>/platform/postgres/migrations/*.cs` file names (no `.Designer.cs`, no snapshot), from the
    AppHost's sibling app folders (`DeployOperate.FindSourceMigrationFolders`, nearest ancestor of the AppHost
    with an app holding one). No folder (no `postgres`) skips the check.
  - Script: the `'<MigrationId>'` of each `INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES (…)` (checked against a real published kubernetes script: 10 ids, latest `20260923180147_AddCredentialAccountHint`).
  - Stale prints the warning (yellow, stderr) before the prompt.
- **Decision (`--yes`):** interactive runs warn and continue only when confirmed; `--yes` refuses (exit 1) on a
  stale script unless the new `--allow-stale` is passed; redirected stdin without `--yes` still refuses as before.
- After a stale warning, a successful report reads `Up to date relative to the published script: …` /
  `Applied N migration(s) relative to the published script:`.
- **Decision (aca bundle):** the bundle's migrations are compiled in and not listable without EF internals, but
  the AppHost calls `PublishAsMigrationScript()` for every target, so the same publish writes
  `efmigrations/web-migrations.sql` beside the bundle; the check reads that script. With no script beside the
  bundle it prints "Stale check not available for the bundle …; skipped." and continues.
- Recorded in the `DeployOperate` and `DeployMigrateCommand` Design regions and in `tw-deploy` (publish → deploy →
  migrate order, the warning, `--allow-stale`).
- Tests (`tests/tools/dev-cli-tests/deploy-operate-tests.cs`, `StaleScript_Given_` + one AppHost agreement test):
  script id extraction, folder listing (Designer/snapshot excluded), stale vs current, `--yes` refusal vs
  `--allow-stale`, report qualifier, no-migrations-folder skip, and the real AppHost resolving the web folder.
- Gates: `dev build` 0 warnings / 0 errors; dev-cli-tests 226/226; `ganda repo audit` passes; `dev check-version` OK.

### How to validate

Smoke:

```bash
dotnet run --project tests/tools/dev-cli-tests/dev-cli-tests.csproj
dotnet run tools/dev-cli/dev.cs -- deploy migrate --help
```

Expect: all tests pass (226, including `StaleScript_Given_`); help lists `--allow-stale`. With an older
`artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql` than the source, `dev deploy migrate
--target kubernetes --yes` prints `⚠ Published script is stale: source has N migration(s) …` and exits 1 naming
`--allow-stale`; without `--yes` it warns, then asks `Migrate? [y/N]` (operator-only; workers must not run it
against a cluster).

### Review

- Rounds: 1; roster: general; effort 2 (by-diff, 372 lines).
- Final counts: bug 0; suggestion 1 wontfix; nit 0; 0 open.
- Disposition: **accepted-exceptions**. M1 (a second postgres app would see a false stale) is wontfix: the template
  has one postgres app and migrate applies one hard-coded script, so pairing scripts with folders belongs to a
  future multi-database migrate.
- Re-verified in the worktree: dev-cli-tests 226/226; `ganda repo audit` passes.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

- Workers must not touch the maintainer's kind cluster or any database.

## Session

- Created: 2026-10-13 (cockpit, per Steve; found in the live 339 check)
- 2026-10-15: implemented under `ganda task work` (implement node); no cluster or database touched.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T18:49:25Z
- Review oracle: review-claude (claude, model claude-opus-5-5), 1 round, accepted-exceptions — 2026-10-15
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-14T18:50:50Z
