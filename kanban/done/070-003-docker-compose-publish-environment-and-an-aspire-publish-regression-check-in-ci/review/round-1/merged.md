# Round 1 — merged findings
**Date:** 2026-10-06
**Sources:** general

## Counts (final, after fix loop)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 1 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:116
- Description: Documented by-hand migration command fails (`fe_sendauth: no password supplied`) — image enforces scram on the in-container socket and `-T` cannot prompt.
- Suggestion: pass `PGPASSWORD="$POSTGRES_PASSWORD"` via `sh -c`.
- Source: general
- Disposition notes: Design region and task.md Results now use `docker compose exec -T postgres sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql …' < efmigrations/web-migrations.sql`.

### M2 — Severity: suggestion — Status: fixed
- File: program.cs (yarp block)
- Description: `Ingress:Port`/`Ingress:HttpPort` pins applied in publish mode too; a Development publish would emit an invalid `host:host:container` mapping.
- Suggestion: gate the pins to run mode.
- Source: general
- Disposition notes: pins now read only when `IsRunMode`; verified `aspire publish --environment Development` emits the single `${INGRESS_PORT}:5000` mapping.

### M3 — Severity: suggestion — Status: fixed
- File: program.cs (UseMock forward)
- Description: `Authentication__UseMock` forward not run-mode gated; config at publish time could emit mock auth.
- Suggestion: gate on run mode.
- Source: general
- Disposition notes: forward now requires `IsRunMode`; Design region updated.

### M4 — Severity: suggestion — Status: wontfix
- File: program.cs (web-server external endpoints)
- Description: Without the yarp flag no service gets a host port in Compose output.
- Suggestion: keep web-server external when yarp is off, or document.
- Source: general
- Disposition notes: Requirement 2 is "only the ingress is published to the host"; a no-ingress combination exposing web-server directly would publish an un-fronted port. Documented in the AppHost Design region: a combination without the yarp flag publishes no host port and the operator adds one for their own edge proxy. Decided by review orchestrator.

### M5 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/publish-compose-command.cs:13
- Description: Design region claimed tools/ does not ship in generated apps (it does).
- Disposition notes: sentence corrected.

### M6 — Severity: nit — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/compose-publish-tests.cs:179
- Description: `.env` empty-secret loop was tautological; URI-embedded literal passwords not caught.
- Disposition notes: tautological loop removed; added `UriCredential()` GeneratedRegex check requiring `${…}` placeholders for `scheme://user:password@` credentials (exercised by POSTGRES_DB_URI).

## Duplicates / conflicts

- None.
