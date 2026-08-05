# Role resolution DB failure must not surface as 401 (fail-closed hardening)

## Description

Spun out of task 158 (its general-hardening half; the 401-vs-403 test failure itself was fixed
there — missing `AuthSchemes` on generated endpoints, commit `6442b605`).

The authorization role-resolution path is structurally capable of mislabeling a DB failure as an
authentication failure: `IClaimsTransformation` → `PrincipalRoleClaimsTransformation`
(`source/container-apps/web/features/admin/principals/principal-role-claims-transformation-server.cs`)
→ `EffectiveRolesResolver.GetEffectiveRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs`)
→ `EfPrincipalRoleStore.GetRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/ef-principal-role-store-infrastructure.cs`),
which queries Postgres. If that EF query throws (connection pool exhaustion, network blip,
Postgres failover, missing schema), an otherwise-successfully-authenticated principal must NOT be
presented as "not authenticated" (401). It should fail closed with an honest status.

## Requirements

- **Decide the fail-closed behavior with the maintainer before implementing** —
  503/5xx (surface the infrastructure failure honestly) vs 403-as-no-roles (treat unresolvable
  roles as no grants). Grok's investigation input (task 158 Notes): prefer **5xx** — a DB outage
  is not an authorization verdict, and 403-as-no-roles hides real infrastructure failures from
  operators while confusing users with a permissions message.
- Implement so a role-store failure for an authenticated principal produces the chosen status,
  never 401.
- Deterministic test: DI-substitute a failing `IPrincipalRoleStore` fake in an in-proc suite
  (do NOT depend on racing real Postgres to reproduce).
- Reconcile `#region Design` blocks in touched files.

## Checklist

- [ ] Confirm fail-closed behavior with maintainer (5xx vs 403-as-no-roles; Grok input: 5xx)
- [ ] Implement in the claims-transformation / resolver path
- [ ] Deterministic in-proc test with failing IPrincipalRoleStore fake (authenticated principal
      → chosen status, not 401)
- [ ] Reconcile Design regions
- [ ] Results with How to validate

## Notes

- Origin: task 158 (root-cause investigation and full evidence chain live there; see its
  "Root-cause investigation (Grok)" Notes section and Results).
- Behavior context: today an exception inside `IClaimsTransformation` propagates through the
  authentication middleware — verify exactly where it turns into 401 vs 500 as part of
  implementation (Grok classified the mechanism; re-verify at implement time).

## Session

- Created: Claude (2026-08-05), spun out of task 158 per maintainer direction.
