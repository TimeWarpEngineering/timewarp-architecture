# Add missing endpoint tests for ListAgentHumanLinks, UpdateRole, DeleteRole

## Description

Definition of Done gaps surfaced by the 210 round-1 code review of the architecture template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`,
findings M12, M13, M14, M15). AGENTS.md Definition of Done requires happy-path AND
validation-rejection tests for every API endpoint; these three are missing coverage.

## Requirements

- New tests are **co-located Jaribu runfiles** per the `tw-feature-placement` skill's
  runfile authoring preamble (`<name>[-<function>]-tests.cs` under `features/`,
  standalone via `dotnet run`). Assertions: **Shouldly** only.
- Every endpoint gets happy-path AND validation-rejection coverage.

### M12

- File: `source/container-apps/web/features/agent-links/list-agent-human-links/list-agent-human-links-contracts.cs`
- Hosted `[ApiEndpoint]` (`GET api/agent-links`) with a handler and zero test coverage
  anywhere (only SPA client state references it).
- Fix: extend `agent-human-link-tests.cs` (or add a co-located runfile) covering both the
  human-session and agent-token paths its Design region describes.

### M13

- Files: `source/container-apps/web/features/admin/roles/update-role/update-role-contracts.cs:34-41`;
  `source/container-apps/web/features/admin/roles/delete-role/delete-role-contracts.cs:37-42`
- Both validators carry real rules (`RoleId` `NotEmpty`, `RoleDetailsValidator`) but have no
  rejection tests; only happy-path coverage exists in `roles-endpoint-tests.cs`. `CreateRole`
  has a dedicated validator test — these two do not.
- Fix: add `update-role-validator-tests.cs` and `delete-role-validator-tests.cs` following
  the `create-role-validator-tests.cs` pattern.

### M14

- Files: `tests/container-apps/web/web-infrastructure-tests/{ef-principal-role-store-tests,ef-principal-store-contract-tests,profile-postgres-persistence-tests}.cs`
- Three copy-pasted `PostgresAvailability` / `ResolveAvailabilityAsync` / `IsCiEnvironment`
  helpers with minor shape drift.
- Fix: extract one shared helper into `tests/common/timewarp-testing` (or a shared file in
  the suite), and update all three call sites to use it.

### M15

- File: `source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:22,84`
- Namespace `TimeWarp.Architecture.Task205` is named after a kanban task; it ships in the
  product tree and means nothing outside this repo's history.
- Fix: rename to a behavior-based namespace (e.g.
  `TimeWarp.Architecture.Features.Identity.ProgressiveProfileGate`).

## Checklist

- [x] M12: `ListAgentHumanLinks` happy-path + validation-rejection tests (human-session and
      agent-token paths)
- [x] M13: `update-role-validator-tests.cs` added
- [x] M13: `delete-role-validator-tests.cs` added
- [x] M14: shared `PostgresAvailability` helper extracted into `tests/common/timewarp-testing`;
      all three call sites updated
- [x] M15: `TimeWarp.Architecture.Task205` renamed to a behavior-based namespace
- [x] `dotnet run` on each new/changed runfile individually
- [x] `dev test`

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.
- M12: dedicated co-located runfile rather than extending `agent-human-link-tests.cs` so the
  operation lives beside its contract/handler. `ListAgentHumanLinks.Validator` has no field
  rules; rejection coverage is unauthenticated 401 plus IDOR (unrelated principal sees nothing).
- M13: Shouldly assertions (task requirement) rather than FluentValidation.TestHelper used by
  the integration-tree `create-role-validator-tests.cs`. Pattern matches co-located
  `create-role-tests.cs` validator class.

## Session

- Created: 223624 (2026-09-12)
- Implementer: grok session 01a095bd-2fcc-71c3-bd6a-149cdd9c41b0 (2026-09-12)

## Results

Closed 210 round-1 findings M12–M15 on this task.

- **M12:** Co-located `list-agent-human-links-tests.cs` — empty query passes the empty validator;
  unauthenticated caller returns 401; human and agent callers each see only links they are a
  party to; an unrelated principal sees none.
- **M13:** Co-located `update-role-validator-tests.cs` (empty RoleId / Name / Description /
  UserId) and `delete-role-validator-tests.cs` (empty RoleId / UserId), plus valid-command
  happy path on both.
- **M14:** `PostgresTestAvailability` in `tests/common/timewarp-testing`; all three
  web-infrastructure-tests call sites use it. `ConnectionString` is as-is (profile tests);
  `AdminConnectionString` rewrites `Database=postgres` (EF store tests).
- **M15:** Namespace `TimeWarp.Architecture.Task205` →
  `TimeWarp.Architecture.Features.Identity.ProgressiveProfileGate`.
- Parent ledger M12–M15 Status set to **fixed**.
- Template smoke web aggregator expected count 135 → 148 (+13 tests).

Files changed:

- `source/container-apps/web/features/agent-links/list-agent-human-links/list-agent-human-links-tests.cs` (new)
- `source/container-apps/web/features/admin/roles/update-role/update-role-validator-tests.cs` (new)
- `source/container-apps/web/features/admin/roles/delete-role/delete-role-validator-tests.cs` (new)
- `source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs`
- `tests/common/timewarp-testing/postgres-test-availability.cs` (new)
- `tests/common/timewarp-testing/timewarp-testing.csproj`
- `tests/container-apps/web/web-infrastructure-tests/ef-principal-role-store-tests.cs`
- `tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs`
- `tests/container-apps/web/web-infrastructure-tests/profile-postgres-persistence-tests.cs`
- `tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj`
- `tools/dev-cli/services/template-smoke-harness.cs`
- `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`

Test outcomes: standalone runfiles passed (5 / 5 / 3 / 4). `web-jaribu-tests` 148/148.
`web-infrastructure-tests` 48/48. `dotnet run tools/dev-cli/dev.cs -- test` completed
successfully (Release, all `tests/` projects).

### How to validate

**Smoke**

```bash
dotnet run source/container-apps/web/features/agent-links/list-agent-human-links/list-agent-human-links-tests.cs
dotnet run source/container-apps/web/features/admin/roles/update-role/update-role-validator-tests.cs
dotnet run source/container-apps/web/features/admin/roles/delete-role/delete-role-validator-tests.cs
dotnet run source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs
```

**Expect:** 5 passed, 5 passed, 3 passed, 4 passed. No failures.

**Automated gate**

```bash
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release
# expect: succeeded: 148, failed: 0

cd tests/container-apps/web/web-infrastructure-tests && dotnet test -c Release
# expect: succeeded: 48, failed: 0

dotnet run tools/dev-cli/dev.cs -- test
# expect: "Tests completed successfully!"
```

**Depends on:** live Postgres infra tests need Docker or
`PostgresDbOptions__ConnectionString` / `ConnectionStrings__postgres-db`. Interactive hosts
without either soft-skip; CI (`CI` or `GITHUB_ACTIONS`) fails closed.

**Not in scope:** HTTP-host coverage of `GET api/agent-links` (handler-level with
`ICurrentPrincipalAccessor` stub covers the dual human/agent paths). Template-smoke tier 3
count bump is for a generated-app run, not this session.
