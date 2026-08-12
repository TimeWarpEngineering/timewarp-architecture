# dev db subcommand group: update reset drop status

## Description

Fold DB operations under `dev db …` against the running AppHost (dynamic postgres port).

## Checklist

- [x] `dev db update` (+ `db-update` alias)
- [x] `dev db reset --yes` (drop + migrate)
- [x] `dev db drop --yes`
- [x] `dev db status`
- [x] Shared DbAppHost runner (aspire resource web-migrations …)
- [ ] Commit

## Session

- Nuru: `[NuruRouteGroup("db")]` + single-literal routes (not multi-word NuruRoute).
- Aspire commands: ef-database-update | reset | drop | status.
