# PrincipalRoleClaimsTransformation should not surface a DB failure as 401 Unauthorized

## Description

Found while working task 155 (AppHost web-server restart deadlock / hybrid on-demand
migrations). `aspire-tests`' `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
(`tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`) returns `401 Unauthorized`
instead of the expected `403 Forbidden` for a mock principal that is authenticated but not an
admin.

**Important correction (2026-08-05):** this was initially diagnosed as a migration-timing race
introduced by task 155 (web-server serving before `RunDatabaseUpdateOnStart` finishes). That
diagnosis is **disproven**: the same failure reproduces identically (3/3 runs) on the
**unmodified baseline** — original `WaitFor(webMigrations)`, none of task 155's changes applied
— where migrations are long finished by the time any test request fires. This is a **pre-existing
bug on `dev`, unrelated to task 155's wait-edge changes**, not something task 155 introduced or
is blocked by. Root cause is genuinely unknown — do not assume it's DB/migration-timing related
without re-investigating from scratch.

What's still true and worth fixing regardless of what turns out to cause *this specific* test
failure: the authorization code path is structurally capable of mislabeling a DB failure as an
authentication failure. Mock authentication itself is pure in-memory
(`source/container-apps/web/platform/identity-host/mock-identity-principal-handler-server.cs`) —
no DB access. Role resolution for authorization runs through `IClaimsTransformation` →
`PrincipalRoleClaimsTransformation`
(`source/container-apps/web/features/admin/principals/principal-role-claims-transformation-server.cs`)
→ `EffectiveRolesResolver.GetEffectiveRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs`)
→ `EfPrincipalRoleStore.GetRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/ef-principal-role-store-infrastructure.cs`),
which queries Postgres directly. If that EF query throws for any reason (missing schema,
connection pool exhaustion, network blip, Postgres failover), whether or not that's what's
actually happening in the failing test today, a DB failure there should not present as "not
authenticated" — it should fail closed as 403 or surface as a genuine 5xx. That's a real,
independent hardening gap worth fixing on its own merits.

## Requirements

- **First: find the actual root cause** of `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
  returning 401 instead of 403 through the closed-box `aspire-tests` ingress path. Candidates to
  investigate fresh (none confirmed): mock-auth header not honored under the `Production`
  environment the closed-box AppHost boots as (confirmed via earlier task-155 log:
  `EnvironmentName: Production`); YARP not forwarding the `X-TimeWarp-Mock-Principal-Id` header
  end-to-end; a genuine bug in `EfPrincipalRoleStore`/`PrincipalRoleClaimsTransformation`
  unrelated to timing; something else entirely. Do not reuse the migration-race explanation —
  it's ruled out.
- **Separately:** a DB failure inside `PrincipalRoleClaimsTransformation` / `EffectiveRolesResolver`
  / `EfPrincipalRoleStore` must not present as 401 Unauthorized for an otherwise-successfully
  -authenticated principal. Decide and implement the correct fail-closed behavior (403-as-no-roles
  vs. propagate-as-5xx) — agree with the maintainer before implementing, don't assume.
- Add/extend a test proving the corrected status code under a simulated role-store failure
  (e.g. inject a failing `IPrincipalRoleStore` fake in an in-proc suite — do not depend on
  racing real Postgres migrations to reproduce this).

## Checklist

- [ ] Reproduce `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` in
      isolation and find the actual root cause (see candidates above) — this is currently unknown
- [ ] Confirm with maintainer which fail-closed behavior is wanted for the general DB-failure
      mislabeling (403-as-no-roles vs. propagate-as-5xx) before implementing
- [ ] Implement the fix(es) — root cause of the failing test, and/or the general hardening,
      depending on what the root-cause investigation finds
- [ ] Add a deterministic test (DI-substituted failing store, not a live-DB race) for the
      general hardening
- [ ] Reconcile any `#region Design` blocks touched
- [ ] Results with How to validate

## Notes

- Related: task 155
  (`kanban/in-progress/155-apphost-web-server-restart-deadlocks-on-finished-web-migrations-waitfor.md` /
  eventual `done/`) — hybrid on-demand migrations; this failing test was found there, but task
  155 does not cause it and is not blocked by it. Task 155's `ingress-smoke-tests.cs` SetupOnce
  does now wait for `web-migrations` to reach a terminal state before firing requests (defensive,
  closes the *actual* migration-race window for this suite) — that wait is harmless and worth
  keeping, but it did not fix this test, confirming the cause lies elsewhere.
- This is scoped independently of 155's AppHost/orchestration changes — no AppHost or migration
  wiring should need to change for this fix.

## Session

- Created: Claude (2026-08-05), spun out of task 155 architecture discussion with maintainer
  (Steve) — agreed as follow-up scope, not blocking 155's close.
- 2026-08-05: corrected root-cause claim after baseline verification (stash + rerun on
  unmodified `dev`) showed the failure predates and is unrelated to task 155's changes; migration
  -race theory disproven, root cause reopened as unknown.
