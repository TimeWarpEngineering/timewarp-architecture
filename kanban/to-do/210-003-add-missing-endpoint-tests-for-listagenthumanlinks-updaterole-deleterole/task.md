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

- [ ] M12: `ListAgentHumanLinks` happy-path + validation-rejection tests (human-session and
      agent-token paths)
- [ ] M13: `update-role-validator-tests.cs` added
- [ ] M13: `delete-role-validator-tests.cs` added
- [ ] M14: shared `PostgresAvailability` helper extracted into `tests/common/timewarp-testing`;
      all three call sites updated
- [ ] M15: `TimeWarp.Architecture.Task205` renamed to a behavior-based namespace
- [ ] `dotnet run` on each new/changed runfile individually
- [ ] `dev test`

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 223624 (2026-09-12)
