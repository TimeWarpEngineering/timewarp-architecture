# Fix namespace and placement violations at the features/platform boundary

## Description

Namespace and placement findings from the 210 round-1 code review of the architecture
template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`,
findings M3–M10). M8 is decided (see Requirements): split — engine and payment port to `platform/`, permission ids
stay substrate.

## Requirements

### Decision (M8) — decided, split

**Decided by Steve, 2026-09-12: split by the `tw-feature-placement` litmus.** No rule was violated;
both folders legally use the Features-substrate exception, but the skill defines that tier as
"compile-time constants or shapes many product slices must share (role ids, module ids)", and
`authorization/` is an 18-file runtime subsystem while `payment/` is a port + HttpContext adapter
(the same shape as `ICurrentPrincipalAccessor` in `platform/identity-host/`).

- **Engine → platform.** Move to `source/container-apps/web/platform/authorization/` with a
  non-Features platform namespace: `permission-evaluator-application.cs`,
  `i-permission-evaluator-application.cs`, `i-role-permission-store-application.cs`,
  `in-memory-role-permission-store-application.cs`, `ef-role-permission-store-infrastructure.cs`,
  `role-permission-grant-infrastructure.cs`,
  `role-permission-grant-entity-type-configuration-infrastructure.cs`,
  `role-permission-seed-application.cs`, `agent-scope-permission-seed-application.cs`,
  `admin-lockout-guards-application.cs`, `i-agent-permission-scope-source-application.cs`,
  `permission-requirement-authorization-server.cs`, `permission-policy-registration-contracts.cs`,
  and the co-located `permission-evaluator-tests.cs` / `permission-claim-policies-tests.cs`.
  M4's file (`agent-caller-permission-scope-source-server.cs`) lands here too, beside its
  interface.
- **Ids stay substrate.** `permission-ids-contracts.cs`, `permission-requirement-contracts.cs`
  and `permission-ids-tests.cs` are product data (which permissions exist) and fit the substrate
  definition as written; they stay under `features/authorization/` in the bare `…Features`
  namespace. If that leaves `features/authorization/` holding only ids, consider renaming the
  folder to match the sibling substrate homes (`features/admin/roles/role-ids-contracts.cs`
  pattern) — implementer's call, record it in Notes.
- **Payment → platform.** Move `i-payment-http-context-application.cs` and
  `http-payment-http-context-server.cs` to `source/container-apps/web/platform/payment/` with
  a platform namespace.
- Reconcile every moved file's Design region (they currently say "Features substrate (not a
  product slice)"); update `tw-feature-placement` if it lists any of these as substrate
  examples; TWA0009 treats platform as one-way free, so slice consumers need no opt-out.

### M3

- File: `source/container-apps/api/platform/identity-host/agent-token-authentication-handler-server.cs:16`
- Platform-cluster file declares the product-slice namespace
  `TimeWarp.Architecture.Features.Identity`. AGENTS.md: "platform clusters keep non-Features
  namespaces". Its web sibling in `web/platform/identity-host/` correctly uses
  `…Web.Server`.
- Fix: rename the namespace to a platform namespace (e.g. `TimeWarp.Architecture.Api.Server`)
  or move to an `api/features/identity/` slice if it is really product code.

### M4

- File: `source/container-apps/web/platform/identity-host/agent-caller-permission-scope-source-server.cs:11`
- Platform-cluster file declares the bare Features-substrate namespace
  `TimeWarp.Architecture.Features`; the substrate tier is documented as features-tree-only and
  this is the sole `platform/**` file using any Features namespace.
- Fix: move into `web/features/authorization/` beside its consumer
  `IAgentPermissionScopeSource`, or give it a platform namespace. Decide together with M8's
  outcome — if `authorization/` moves to `platform/authorization/`, this file lands there
  too.

### M5

- Files: `source/container-apps/web/features/todo-items/delete-todo-item/delete-todo-item-contracts.cs:11`;
  `source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs:15`
- Only file in the slice with a `.Commands` sub-namespace (message-kind grouping, the
  documented anti-pattern); hand-rolled `GetHttpVerb`/`GetRoute` instead of `[ApiRoute]`; no
  `[ClientOnlyContract]` although no server endpoint or handler exists (its siblings
  `create-todo-item`/`update-todo-item` carry the marker). Sibling DTO file carries a
  redundant, typo'd `TODO: Revist the Mixins` already answered by its own Design region.
- Fix: drop `.Commands`; either adopt `[ApiRoute]` + `[ClientOnlyContract(reason)]` like its
  siblings or state in Design why it stays hand-rolled; delete the DTO TODO line.

### M6

- File: `source/container-apps/web/projects/web-server/hosted-identity-session-authentication-state-provider-server.cs`
- Real identity/prerender concern sitting at the artifact-folder root; fails the "would it
  still mean something if the deployable were deleted" litmus. Its named sibling
  `identity-session-cookie-forwarding-server.cs` lives in `web/platform/identity-host/`.
- Fix: move to `web/platform/identity-host/` (filename is already grammar-conformant).

### M7

- Files: `source/container-apps/web/projects/web-spa/pages/{AgentLinksPage,ProfilePage,SettingsPage}.razor(.cs)`
  (six files)
- Project-root `pages/` folder whose files' own namespaces are `Features.AgentLinks`,
  `Features.Profiles`, `Features.Applications`; those feature folders already exist, and
  identity/application already keep pages under `features/<slice>/pages/`.
- Fix: move each into its feature's `pages/` folder and delete the root `pages/` folder.

### M9

- File: `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs:170-220`
- TWA0015/0016 only match `/{family}/features/` paths; `platform/` is never
  function-pair-checked (the membership guard still enforces layer suffixes there). AGENTS.md
  and the `tw-feature-placement` skill describe the full grammar as covering both trees.
  Scoping is probably deliberate (avoids false positives on ASP.NET
  `AuthenticationHandler`-named files) but is undocumented.
- Fix: record the features-only scope in the analyzer's Design region and the skill, or
  extend to `platform/` with an allowance for `*-handler-server.cs` when the type derives
  from `AuthenticationHandler`.

### M10

- File: `AGENTS.md` kebab exception table
- `SKILL.md` and `AnalyzerReleases.{Shipped,Unshipped}.md` are tooling-mandated uppercase
  basenames not listed in the exception table.
- Fix: add both to the table.

## Checklist

- [x] M8 decided (split; see Requirements → Decision)
- [x] M8: authorization engine + payment port moved to `platform/`, ids left as substrate, Design regions reconciled
- [x] M3: `agent-token-authentication-handler-server.cs` namespace fixed
- [x] M4: `agent-caller-permission-scope-source-server.cs` moved to `platform/authorization/` with platform namespace
- [x] M5: `delete-todo-item-contracts.cs` / `todo-item-dto-contracts.cs` fixed
- [x] M6: `hosted-identity-session-authentication-state-provider-server.cs` moved to
      `web/platform/identity-host/`
- [x] M7: the six `pages/` files moved into their feature folders; root `pages/` deleted
- [x] M9: TWA0015/0016 features-only scope documented, or extended to `platform/`
- [x] M10: `SKILL.md` / `AnalyzerReleases.*.md` added to the kebab exception table
- [x] `dev build` 0/0 (full rebuild — analyzer/generator changes can go stale under
      incremental builds)
- [x] `dev test`
- [x] `dev template-smoke`
- [x] Implementation review (effort 1, general): 2 rounds, disposition clean

## Notes

- M8 decision recorded 2026-09-12 (Steve, cockpit session
  https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED): split engine/platform vs ids/substrate.

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  M3–M10 Status set to fixed on that ledger (same PR).

- Ids folder: kept `features/authorization/` (three files: `permission-ids-contracts.cs`,
  `permission-requirement-contracts.cs`, `permission-ids-tests.cs`). Not folded into
  `features/admin/roles/` — permission ids are a cross-slice catalog, not role-slice-owned.
  Sibling `role-ids-contracts.cs` lives in roles because those Guids *are* the roles slice.

- Namespaces: engine `TimeWarp.Architecture.Authorization`; payment
  `TimeWarp.Architecture.Payment`; M3 `TimeWarp.Architecture.Api.Server`. Cluster-named,
  matching `Abuse` / `AgentDiscovery`.

- M9: documented features-only TWA0015/0016 pairing rather than extending to `platform/`
  (AuthenticationHandler `*-handler-server.cs` would false-positive). Membership guard
  still requires layer suffixes on both trees.

- Review kitchen: `review/` under this folder (framework, round-1, round-2, disposition).
  Disposition **clean** — see Results → Review disposition.

## Session

- Created: 222278 (2026-09-12)
- Implementer: grok session 01a095bd-66cd-75c3-91e6-0daf89bc14bc (2026-09-12)
- Review oracle: grok session 01a095dd-3879-7d10-b70c-cd2e1c38c7b7 (2026-09-12)
- Review general round 1: grok subagent 01a095df-4308-7b71-aa7b-79882ec2cbab (2026-09-12)
- Review general round 2: grok subagent 01a095e3-851a-7713-9943-13328d9af4ac (2026-09-12)

## Results

M3–M10 placement/namespace findings from the 210 round-1 review. Engine and payment port
moved to `platform/` with non-Features namespaces; permission ids stay Features substrate;
SPA pages rehomed; TWA0015/0016 features-only scope documented; kebab exceptions listed.

### What was implemented

- **M8 / M4.** Authorization runtime (evaluator, stores, seeds, policy registration, handler,
  agent-scope adapter) → `web/platform/authorization/` namespace
  `TimeWarp.Architecture.Authorization`. Payment HttpContext port →
  `web/platform/payment/` namespace `TimeWarp.Architecture.Payment`. Design regions
  rewritten (no longer claim Features substrate). `tw-feature-placement` substrate vs
  platform litmus updated. How-to PDP swap path updated. Template `(!postgres)` exclude
  list retargeted at the new EF store paths.
- **M3.** `agent-token-authentication-handler-server.cs` → `TimeWarp.Architecture.Api.Server`.
- **M5.** `DeleteTodoItem` dropped `.Commands`; `[ApiRoute]` + `[ClientOnlyContract]` like
  create/update. DTO TODO line deleted.
- **M6.** Hosted identity-session auth-state provider → `web/platform/identity-host/`.
- **M7.** AgentLinks / Profile / Settings pages into their feature `pages/` folders;
  root `web-spa/pages/` deleted.
- **M9.** Analyzer Design region + skill + AGENTS.md TWA table + analyzer test
  `Given_Platform_Handler_Server_IsSilent`.
- **M10.** `SKILL.md` and `AnalyzerReleases.{Shipped,Unshipped}.md` on the AGENTS.md kebab
  exception list; `SKILL.md` also on `file-naming.md`.

### Files changed

Primary trees: `web/platform/authorization/`, `web/platform/payment/`,
`web/features/authorization/` (ids only), `web/platform/identity-host/`,
`web-spa/features/{agent-links,profiles,application}/pages/`, api identity-host,
todo-item contracts, analyzer + tests, AGENTS.md, `tw-feature-placement`,
`.template.config/template.json`, parent 210 round-1 ledger,
`PostgresDbContextModelSnapshot.cs` (review M1 snapshot FQN).

### Key decisions

- Keep `features/authorization/` as the human folder for the permission-id catalog (see Notes).
- Document TWA0015/0016 features-only scope rather than extend pairing to `platform/`.
- Review M1: retarget living snapshot CLR name only; do not rewrite historical Designers.

### Test outcomes

- `dotnet run tools/dev-cli/dev.cs -- build --clean` then `build`: 0 warnings / 0 errors
- `dotnet run tools/dev-cli/dev.cs -- test`: pass (web-jaribu-tests 135/135 including moved
  permission-evaluator/claim-policies runfiles; analyzer tests 158/158 including platform-silent)
- `dotnet run tools/dev-cli/dev.cs -- template-smoke`: SUCCEEDED (SmokeDefault, SmokeNoPostgres,
  SmokeNoApi)

### How to validate

**Smoke**

```bash
cd /path/to/timewarp-architecture   # this task worktree or a fresh claim
dotnet run tools/dev-cli/dev.cs -- build --clean
# Expect: "Build completed successfully!" and 0 Warning(s) / 0 Error(s)

test ! -d source/container-apps/web/projects/web-spa/pages
test -f source/container-apps/web/platform/authorization/i-permission-evaluator-application.cs
test -f source/container-apps/web/features/authorization/permission-ids-contracts.cs
test -f source/container-apps/web/platform/payment/i-payment-http-context-application.cs
rg -n '^namespace TimeWarp.Architecture.Api.Server;' \
  source/container-apps/api/platform/identity-host/agent-token-authentication-handler-server.cs
rg -n '^namespace TimeWarp.Architecture.Authorization;' \
  source/container-apps/web/platform/authorization/i-permission-evaluator-application.cs
rg -n 'namespace TimeWarp.Architecture.Features.TodoItems;' \
  source/container-apps/web/features/todo-items/delete-todo-item/delete-todo-item-contracts.cs
rg -n 'Authorization.RolePermissionGrant' \
  source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs
```

**Expect**

- `web-spa/pages/` is gone.
- Evaluator lives under `platform/authorization/` with namespace
  `TimeWarp.Architecture.Authorization`.
- `permission-ids-contracts.cs` remains under `features/authorization/` in
  `TimeWarp.Architecture.Features`.
- Payment port is under `platform/payment/`.
- Api agent-token handler namespace is `TimeWarp.Architecture.Api.Server`.
- DeleteTodoItem is `Features.TodoItems` (not `.Commands`) and uses `[ApiRoute]`.
- Living EF snapshot names `TimeWarp.Architecture.Authorization.RolePermissionGrant`.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- test
# Expect: "Tests completed successfully!"

dotnet run source/container-apps/web/platform/authorization/permission-evaluator-tests.cs
# Expect: all tests passed (host-free evaluator + seed coverage)

dotnet run tools/dev-cli/dev.cs -- template-smoke
# Expect: "Template smoke SUCCEEDED" including SmokeNoPostgres
```

**Not in scope:** live PDP swap (OpenFGA); browser click-through of the rehomed SPA pages
(namespaces and `[Page]` routes unchanged).

### Review disposition

- **Disposition:** clean
- **Effort / roster:** 1, general only
- **Rounds:** 2
- **Final counts:** bug 0/0/0 open/fixed/wontfix; suggestion 0 open / 1 fixed / 0 wontfix; nit 0/0/0
- **M1 (suggestion, fixed):** living `PostgresDbContextModelSnapshot` entity name retargeted from `TimeWarp.Architecture.Features.RolePermissionGrant` to `TimeWarp.Architecture.Authorization.RolePermissionGrant`. Historical `*.Designer.cs` left unchanged.
- **Paths:** `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/merged.md`, `review/disposition.md`
