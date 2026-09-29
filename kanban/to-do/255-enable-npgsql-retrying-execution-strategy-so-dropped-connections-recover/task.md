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

- [x] EnableRetryOnFailure with explicit bounds (runtime DbContext)
- [x] Explicit transaction in the principal role store wrapped in the execution strategy; others audited
- [x] Concurrency exceptions not retried by the strategy (confirmed)
- [x] Terminated-connection recovery test + transaction tests
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Results

- **Policy:** new `PostgresRetryPolicy` (`source/container-apps/web/platform/postgres/postgres-retry-policy-infrastructure.cs`)
  — `EnableRetryOnFailure(maxRetryCount: 4, maxRetryDelay: 2s, errorCodesToAdd: null)`; Npgsql's own
  transient classification (IO/socket, 57P01, 40001, 40P01) is the retry set. `PostgresDbModule`
  applies it via `UseNpgsql(connectionString, PostgresRetryPolicy.Configure)`. It lives in
  infrastructure so the web-infrastructure-tests build contexts with the exact production policy.
- **Design-time factory:** intentionally unchanged (no retries) — `dotnet ef` / AppHost migration
  resource are one-shot runs with no stale pool; failures should surface immediately. Recorded in
  both Design regions.
- **Transactions audited:** the only explicit transaction (`EfPrincipalRoleStore.TryClaimFirstAdministratorAsync`,
  Serializable) now runs begin → read → write → commit inside `Database.CreateExecutionStrategy().ExecuteAsync`;
  the replay re-reads "Administrator exists?" inside its own transaction and clears the change tracker
  on retry attempts so stale Added/accepted rows cannot collide. No `TransactionScope` anywhere; every
  other store method is a single `SaveChanges` unit (EF wraps those in the strategy itself). Handlers
  that call several store methods were already non-atomic (autocommit per call) — semantics unchanged.
  Known edge (documented in the role-store Design region): a commit whose ack is lost replays into
  "Administrator exists" and returns false for the actual winner; the grant is durable either way.
- **Concurrency:** `DbUpdateConcurrencyException` is raised by EF from a rows-affected mismatch and
  carries no transient `NpgsqlException`, so the strategy does not retry it — the stores' store-CAS
  translation and the app's optimistic-concurrency loops (RevokeCredential, RenameCredential,
  CredentialUsageRecorder, SiteSettings seeder) see it on the first attempt. Confirmed by the
  principal-store contract and site-settings concurrency tests, which now run with the policy enabled
  and stay green.
- **Tests:** new `tests/container-apps/web/web-infrastructure-tests/postgres-connection-recovery-tests.cs`
  (per-test database, real Postgres/Testcontainers):
  - control — without the policy, `pg_terminate_backend` between two queries makes the second throw;
  - with the policy, the second query after the terminate succeeds;
  - claim transaction: an interceptor kills the backend inside the serializable transaction; the
    claim still returns true and both Administrator + Member rows persist;
  - four concurrent claims under the policy elect exactly one Administrator (40001 losers replay into `false`).
  Existing EF store suites (role store, principal-store contract, site settings, profile persistence)
  now build contexts with `PostgresRetryPolicy.Configure`.
- **Gates:** `dev build` 0 warnings / 0 errors; `dev test` all suites green (web-infrastructure-tests
  61/61; web-server-integration-tests 276 + 1 pre-existing skip); `CI=1` run of the new class 4/4 (no
  soft-skip); `dev template-smoke` SUCCEEDED including SmokeNoPostgres; `ganda repo audit` clean
  (after `--fix --checks bin-dev` installed the local `bin/dev`, untracked).
- **Not performed:** the manual `dev db reset` → load site check — no AppHost was started (per brief).

### How to validate

- **Smoke:** `cd tests/container-apps/web/web-infrastructure-tests && CI=1 dotnet test -c Release -- --filter-class Connection_recovery`
- **Expect:** 4/4 succeeded, 0 skipped (CI=1 fails closed if neither Docker nor a connection string
  is available). Then `dotnet test -c Release` in the same folder: 61/61. Manual (needs AppHost):
  `dev run`, load the site, `dev db reset`, reload — the page loads instead of the 57P01 error.

## Notes

- Origin: `dev db reset` while running, 2026-09-29 (after task 254).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-29)
