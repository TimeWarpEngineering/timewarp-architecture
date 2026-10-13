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

- [ ] Source vs script migration id comparison; warning text
- [ ] `--yes` refuses on stale unless `--allow-stale`; summary qualifier
- [ ] ACA bundle handling decision recorded
- [ ] Tests + skill note
- [ ] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster or any database.

## Session

- Created: 2026-10-13 (cockpit, per Steve; found in the live 339 check)
