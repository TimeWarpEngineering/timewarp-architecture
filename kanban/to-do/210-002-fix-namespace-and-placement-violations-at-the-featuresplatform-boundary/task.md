# Fix namespace and placement violations at the features/platform boundary

## Description

Namespace and placement findings from the 210 round-1 code review of the architecture
template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`,
findings M3–M10). One human decision (M8) gates the M3–M7 moves; do not start those moves
until M8 is decided.

## Requirements

### Decision (human, M8)

Steve decides whether `source/container-apps/web/features/authorization/` (18 files) and
`source/container-apps/web/features/payment/` (2 files) move to
`source/container-apps/web/platform/authorization/` and
`source/container-apps/web/platform/payment/`, or stay under `features/` with a written
Design-region reason. Every file in both folders uses the bare Features-substrate namespace;
neither has a single slice-scoped file. Structurally they match the `tw-feature-placement`
skill's own worked example of a platform cluster (a seam interface beside its implementation,
shared by several slices). **M4's move target depends on this decision.** Do not start the
M3–M7 moves until M8 is decided; record the decision under Notes below before touching code.

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

- [ ] M8 decided and recorded under Notes (moved to platform/, or kept under features/ with
      a Design-region reason)
- [ ] M3: `agent-token-authentication-handler-server.cs` namespace fixed
- [ ] M4: `agent-caller-permission-scope-source-server.cs` moved/renamed per M8's outcome
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

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 222278 (2026-09-12)
