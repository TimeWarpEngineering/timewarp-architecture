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
- [ ] M8: authorization engine + payment port moved to `platform/`, ids left as substrate, Design regions reconciled
- [ ] M3: `agent-token-authentication-handler-server.cs` namespace fixed
- [ ] M4: `agent-caller-permission-scope-source-server.cs` moved to `platform/authorization/` with platform namespace
- [ ] M5: `delete-todo-item-contracts.cs` / `todo-item-dto-contracts.cs` fixed
- [ ] M6: `hosted-identity-session-authentication-state-provider-server.cs` moved to
      `web/platform/identity-host/`
- [ ] M7: the six `pages/` files moved into their feature folders; root `pages/` deleted
- [ ] M9: TWA0015/0016 features-only scope documented, or extended to `platform/`
- [ ] M10: `SKILL.md` / `AnalyzerReleases.*.md` added to the kebab exception table
- [ ] `dev build` 0/0 (full rebuild — analyzer/generator changes can go stale under
      incremental builds)
- [ ] `dev test`
- [ ] `dev template-smoke`

## Notes

- M8 decision recorded 2026-09-12 (Steve, cockpit session
  https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED): split engine/platform vs ids/substrate.

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 222278 (2026-09-12)
