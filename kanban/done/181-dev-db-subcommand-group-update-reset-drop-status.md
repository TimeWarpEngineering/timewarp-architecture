# dev db subcommand group: update reset drop status

## Description

Fold DB operations under `dev db …` against the running AppHost (dynamic postgres port).

## Checklist

- [x] `dev db update` (+ `db-update` alias)
- [x] `dev db reset --yes` (drop + migrate)
- [x] `dev db drop --yes`
- [x] `dev db status`
- [x] Shared DbAppHost runner (aspire resource web-migrations …)
- [x] Commit (`e7e1921a`)

## Results

Shipped `dev db` as a Nuru route group against the running Aspire AppHost (resource `web-migrations`):

| Command | Action |
|---------|--------|
| `dev db update` / `dev db-update` | Apply pending migrations (`ef-database-update`) |
| `dev db reset --yes` | Drop + recreate with all migrations (`ef-database-reset`) |
| `dev db drop --yes` | Delete the database (`ef-database-drop`) |
| `dev db status` | Current migration status (`ef-database-status`) |

Shared runner: `tools/dev-cli/endpoints/db-app-host.cs`. Group: `[NuruRouteGroup("db")]` on `DbGroup` (not multi-word `NuruRoute`).

Commit: `e7e1921a` — already on `dev` / PR #301.

### How to validate

**Smoke** (AppHost running: `dev run` / `aspire start`)

```bash
dev db --help
# expect: update, reset, drop, status

dev db status
# expect: Aspire resource command output for web-migrations / EF status
```

**Expect**

- `dev db update` maps to Aspire `ef-database-update`.
- Destructive commands require `--yes`.
- Without a running AppHost, commands fail clearly (no silent no-op).

**Automated gate**

```bash
# No dedicated test project; CLI is exercised via help + live AppHost.
dotnet run tools/dev-cli/dev.cs -- db --help
# expect: exit 0 and db subcommands listed
```

**Depends on:** running Aspire AppHost with postgres + `web-migrations`.

**Not in scope:** shipping a standalone postgres without Aspire.

## Session

- Nuru: `[NuruRouteGroup("db")]` + single-literal routes (not multi-word NuruRoute).
- Aspire commands: ef-database-update | reset | drop | status.
- 2026-08-12: marked done — implementation already committed (`e7e1921a`); Results + How to validate added.
