# dev deploy migrate

`dev deploy migrate` applies the published idempotent migrations (safe to re-run) for the
target, after a confirmation (`--yes` skips it; without a terminal and without `--yes` it
refuses), reports which migrations it applied, and exits with the underlying tool's exit code. It runs the
file `dev publish <target>` wrote under `artifacts/aspire-output/<target>/efmigrations/`; when that
is missing it **publishes the files** (`aspire publish` only — the safety suite stays on
`dev publish`) and then continues. A failed publish stops and names `dev publish <target>`.
Per target it runs
what the Postgres and migrations table in the tw-deploy skill lists: compose — the running Compose project from
`<runtime> compose ls` (`--project-name` picks one when several run; the runtime is
`ASPIRE_CONTAINER_RUNTIME`, else docker); kubernetes — `kubectl exec` in the `k8s-namespace`
parameter's namespace on the current context (same preflight as `dev deploy`, minus Helm and the
registry); aca — the bundle through a temporary firewall rule that is **always deleted**, even on
failure (the Azure Container Apps section of the tw-deploy skill). aca-neon runs the same bundle with the direct Neon connection
string and creates no firewall rule (the Azure Container Apps + Neon section of the tw-deploy skill). That migrate does not
require `az login` or the pooled string.
compose and kubernetes read EF's `__EFMigrationsHistory` table before and after the run, through
the same `exec` path (a missing table is a fresh database: none applied), and run the script
quietly (`psql -q`, `PGOPTIONS='-c client_min_messages=warning'`), so only errors and WARNINGs
print. The idempotent script's transcript looks the same whether ten migrations applied or none;
the report says what changed. `--verbose` prints psql's full transcript instead (on aca and
aca-neon, the bundle's `--verbose`); the report still follows. Both bundle targets report from the bundle's own
`Applying migration '…'` lines.

```text
Applied 2 migration(s):
  20261001120000_AddTodoItems
  20261012090000_AddRoles
Already applied: 5 · Database now at 20261012090000_AddRoles
dev deploy migrate: applied web-migrations.sql with psql to kubernetes (context kind-app, namespace app).
```

Nothing pending prints `Up to date: no pending migrations (7 applied, latest 20261012090000_AddRoles)`.
A failure prints psql's error, then `Failed while applying migrations after <last applied id>`,
and exits with psql's code.

**Publish, then deploy, then migrate.** "Up to date" is measured against the published script,
not the source. Before the prompt the verb compares the migration ids in the source (the
`<app>/platform/postgres/migrations/` folders beside the AppHost; none — no `postgres` — skips
the check) with the ids in the script's `__EFMigrationsHistory` inserts. When the source has
migrations the script lacks, it warns:

```text
⚠ Published script is stale: source has 3 migration(s) the script does not
  (20261010075816_AddFeedbackAttachments, …). Run `dev publish <target>` first,
  then redeploy so the app and schema match.
```

Interactively you can still confirm; with `--yes` it **refuses** unless `--allow-stale` is
passed, and the report then reads `Up to date relative to the published script: …` /
`Applied N migration(s) relative to the published script:`. aca and aca-neon check the
`web-migrations.sql` the same publish writes beside the bundle (the bundle's own migration list
is compiled in); with no script there it says the check is unavailable and skips it.
