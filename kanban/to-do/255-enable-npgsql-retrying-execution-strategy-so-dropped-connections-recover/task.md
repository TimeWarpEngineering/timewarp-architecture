# Enable Npgsql retrying execution strategy so dropped connections recover

## Description

Reported 2026-09-29: with the AppHost running, `dev db reset` then loading the site gave an error
page — `PostgresException: 57P01: terminating connection due to administrator command`, wrapped by
EF in `InvalidOperationException: An exception has been raised that is likely due to a transient
failure` (`NpgsqlExecutionStrategy.ExecuteAsync`). The reset drops the database, Postgres kills
every open connection, and web-server's Npgsql pool hands out one of the dead connections on the
next request. The web `PostgresDbContext` is registered with a plain
`UseNpgsql(connectionString)` (`source/container-apps/web/platform/postgres/postgres-db-module-server.cs:68`),
so no retrying strategy is configured: EF classifies 57P01 as transient but throws instead of
reconnecting. The same failure would hit production on any Postgres restart or failover.

## Requirements

- Configure `UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(...))` in
  `postgres-db-module-server.cs`, with explicit, modest bounds (e.g. 3–5 retries, short max delay)
  stated in the Design region. Keep the design-time factory
  (`postgres-db-context-design-time-factory-infrastructure.cs`) without retries unless migrations
  need it — record the choice.
- A retrying strategy rejects user-initiated transactions outside `strategy.ExecuteAsync`. The one
  explicit transaction found is `ef-principal-role-store-infrastructure.cs:89`
  (`BeginTransactionAsync(Serializable)`): wrap the whole unit (begin → work → commit) in
  `Database.CreateExecutionStrategy().ExecuteAsync(...)` so a retry replays the full transaction,
  and make sure the retried body is idempotent (re-reads inside the transaction). Grep for any other
  `BeginTransaction`, `TransactionScope`, or multi-`SaveChanges` unit that assumes a single attempt,
  and handle or justify each.
- Interaction with the app's own optimistic-concurrency retry loops (RevokeCredential,
  RenameCredential, CredentialUsageRecorder, SiteSettings seeder): the execution strategy must not
  retry a `DbUpdateConcurrencyException` (it is not transient) — confirm and note it.
- Tests: a Postgres-backed test (web-infrastructure-tests / Testcontainers) that terminates the
  context's backend connection (`pg_terminate_backend`) between two queries and asserts the second
  query succeeds; the serializable role-store transaction still commits and still detects
  conflicts; existing store contract tests stay green.
- Reconcile Design regions (postgres module, principal role store).
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke` (postgres flag off must still build).
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host). Record the
  manual `dev db reset` → load site check as not performed.

## Checklist

- [ ] EnableRetryOnFailure with explicit bounds (runtime DbContext)
- [ ] Explicit transaction in the principal role store wrapped in the execution strategy; others audited
- [ ] Concurrency exceptions not retried by the strategy (confirmed)
- [ ] Terminated-connection recovery test + transaction tests
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Origin: `dev db reset` while running, 2026-09-29 (after task 254).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-29)
